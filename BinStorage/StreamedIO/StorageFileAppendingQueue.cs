using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BinStorage.Common;
using BinStorage.Index;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides asynchronous chain of write requests to the file
    /// </summary>
    internal class StorageFileAppendingQueue
    {
        private readonly IStreamFactory streamFactory;
        private readonly IBinaryStorageIndex binaryStorageIndex;

        private readonly Stream fileStream;
        private readonly IStreamedIOBuffer buffer;
        private readonly Thread preparingThread;
        private readonly Thread processingThread;

        private readonly ConcurrentQueue<AppendRequestDescriptor> waitingList;
        private long pendingCounter;
        private long processedCounter;

        /// <summary>
        /// Initializes new instance of <see cref="StorageFileAppendingQueue"/> class.
        /// </summary>
        /// <param name="streamFactory"><see cref="IStreamFactory"/> object used to create file streams</param>
        /// <param name="binaryStorageIndex">reference to binary storage index structure</param>
        /// <exception cref="ArgumentNullException">factory is null, index is null</exception>
        /// <exception cref="IOException">storage file could not be opened</exception>
        public StorageFileAppendingQueue(IStreamFactory streamFactory, IBinaryStorageIndex binaryStorageIndex)
        {
            this.binaryStorageIndex = binaryStorageIndex.ThrowIfNull(nameof(binaryStorageIndex));
            this.streamFactory = streamFactory.ThrowIfNull(nameof(streamFactory));

            buffer = streamFactory.CreateStreamedIOBuffer();
            fileStream = streamFactory.CreateStorageAppendingStream();
            ShrinkStorageFile();

            pendingCounter = 0;
            processedCounter = 0;
            waitingList = new ConcurrentQueue<AppendRequestDescriptor>();

            preparingThread = new Thread(DoInternalWrite);
            preparingThread.Start();

            processingThread = new Thread(DoBufferProcessing);
            processingThread.Start();
        }

        /// <summary>
        /// Appends stream to storage file, respecting previous append requests
        /// </summary>
        /// <param name="data">stream to be written to the file</param>
        /// <param name="streamMetadata">stream Metadata related to stream being appended</param>
        /// <param name="token">cancellation token for asynchronous operation</param>
        /// <returns>Task object that will be completed once stream data is written</returns>
        public Task Add(Stream data, StreamMetadata streamMetadata, CancellationToken token)
        {
            if (binaryStorageIndex.Contains(streamMetadata.Key))
            {
                throw new ArgumentException(nameof(streamMetadata.Key));
            }

            streamMetadata.Offset = binaryStorageIndex.TotalLength;

            var descriptor = new AppendRequestDescriptor(data, streamMetadata, token);
            waitingList.Enqueue(descriptor);
            return descriptor.TaskSource.Task;
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        /// <remarks>
        /// Note that FileStream itself is managed resource, that implements IDisposable pattern,
        /// so finalizer is not required in the class that uses it.
        /// </remarks>
        public void Dispose()
        {
            //Wait for write requests to complete
            buffer.Dispose();
            processingThread.Join();
            ShrinkStorageFile();
            fileStream.Dispose();
        }

        private void ShrinkStorageFile()
        {
            var actualLength = fileStream.Length;
            var expectedLength = binaryStorageIndex.TotalLength;
            if (actualLength != expectedLength)
            {
                Console.WriteLine($"Storage file length is {actualLength}. Shrinking to {expectedLength}");
                fileStream.SetLength(expectedLength);
            }
        }

        private Task InternalWrite(Stream data, string key, long length, StreamInfo info, CancellationToken token)
        {
            var streamMetadata = new StreamMetadata()
            {
                Offset = binaryStorageIndex.TotalLength,
                Key = key,
                Length = length,
                IsCompressed = false
            };

            try
            {
                var result = WriteToBuffer(data, token, streamMetadata);
                ValidateInfo(streamMetadata, info);
                binaryStorageIndex.Set(streamMetadata);
                return result;
            }
            catch
            {
                binaryStorageIndex.Skip(streamMetadata.Length);
                throw;
            }
        }

        private Task WriteToBuffer(Stream data, CancellationToken token, StreamMetadata streamMetadata)
        {
            using (var md5 = MD5.Create())
            {
                var expectedLength = streamMetadata.Length;
                streamMetadata.Length = 0;
                int bytesWritten;
                Stream stream = new CryptoStream(data, md5, CryptoStreamMode.Read);
                if (streamMetadata.IsCompressed)
                {
                    //TODO: create GZip stream compression wrapper stream and use it here
                    //Usage: stream = GZipWrapperStream(stream, CompressionMode.Compress, false);
                    //Remarks: wrapper should also provide calculated length of underlying stream
                    //to be stored in streamMetadata UncompressedLength property.
                    //Sadly, GZipStream does not work this way :(
                }

                using (stream)
                {
                    while ((bytesWritten = buffer.WriteToBuffer(stream)) > 0)
                    {
                        streamMetadata.Length += bytesWritten;
                        pendingCounter += bytesWritten;
                        if (streamMetadata.Length > expectedLength)
                        {
                            throw new ArgumentException(nameof(streamMetadata.Length));
                        }
                    }
                }

                streamMetadata.Hash = md5.Hash.ToArray();

                return GetWaitingTask();
            }
        }

        private void ValidateInfo(StreamMetadata streamMetadata, StreamInfo info)
        {
            if (info.Length.HasValue && info.Length != streamMetadata.Length)
            {
                throw new ArgumentException(nameof(info.Length));
            }

            if (info.Hash != null && !info.Hash.SequenceEqual(streamMetadata.Hash))
            {
                throw new ArgumentException(nameof(info.Hash));
            }
        }

        private Task GetWaitingTask()
        {
            var descriptor = new AppendRequestDescriptor() { finishCounter = pendingCounter };
            waitingList.Enqueue(descriptor);
            if (Interlocked.Read(ref processedCounter) >= pendingCounter)
            {
                return null;
            }

            return descriptor.TaskSource.Task;
        }

        private void OnCounterChanged(int processed)
        {
            Interlocked.Add(ref processedCounter, processed);
            AppendRequestDescriptor item;
            while (waitingList.TryPeek(out item))
            {
                if (item.finishCounter > processedCounter)
                {
                    break;
                }

                item.TaskSource.SetResult(null);
                waitingList.TryDequeue(out item);
            }
        }

        /// <summary>
        /// Provides sequential item processing for internal queue
        /// </summary>
        private void DoBufferProcessing()
        {
            while (!cancellationOnDispose.IsCancellationRequested)
            {
                try
                {
                    var processed = buffer.ReadFromBuffer(fileStream);
                    OnCounterChanged(processed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occured during writing to storage file: {ex.Message}");
                }
            }
        }
    }
}