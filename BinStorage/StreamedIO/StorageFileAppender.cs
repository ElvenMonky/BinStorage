using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using BinStorage.Common;
using BinStorage.Index;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides asynchronous chain of write requests to the file
    /// </summary>
    internal class StorageFileAppender : IStorageFileAppender
    {
        private const string NotSupportedLength = "Incoming stream should provide length";
        private const string StorageFileLengthCorrupted = "Storage file length is less than it should be according to index data. Storage might be corrupted.";
        private const int DefaultBufferBlockSize = 0x4000;//16KB

        private readonly CancellationTokenSource cancellationOnDispose;
        private readonly StorageConfiguration configuration;
        private readonly IStreamFactory streamFactory;
        private readonly IBinaryStorageIndex binaryStorageIndex;

        private readonly Stream fileStream;
        private readonly IStreamedIOBuffer buffer;
        private readonly Thread processingThread;

        private readonly ConcurrentQueue<Tuple<long, ManualResetEvent>> waitingList;
        private long pendingCounter;
        private long processedCounter;

        private readonly object writeLock;

        /// <summary>
        /// Initializes new instance of <see cref="StorageFileAppender"/> class.
        /// </summary>
        /// <param name="configuration">The configuration parameters for binary storage components</param>
        /// <param name="streamFactory"><see cref="IStreamFactory"/> object used to create file streams</param>
        /// <param name="binaryStorageIndex">reference to binary storage index structure</param>
        /// <exception cref="ArgumentNullException">factory is null</exception>
        /// <exception cref="IOException">storage file could not be opened</exception>
        public StorageFileAppender(StorageConfiguration configuration, IStreamFactory streamFactory, IBinaryStorageIndex binaryStorageIndex)
        {
            this.configuration = configuration.ThrowIfNull(nameof(configuration));
            this.binaryStorageIndex = binaryStorageIndex.ThrowIfNull(nameof(binaryStorageIndex));
            this.streamFactory = streamFactory.ThrowIfNull(nameof(streamFactory));

            buffer = streamFactory.CreateStreamedIOBuffer();
            fileStream = streamFactory.CreateStorageAppendingStream();
            ShrinkStorageFile();

            pendingCounter = 0;
            processedCounter = 0;
            waitingList = new ConcurrentQueue<Tuple<long, ManualResetEvent>>();

            cancellationOnDispose = new CancellationTokenSource();
            processingThread = new Thread(DoBufferProcessing);
            processingThread.Start();

            writeLock = new object();
        }

        /// <summary>
        /// Appends stream to storage file, respecting previous append requests
        /// </summary>
        /// <param name="data">stream to be written to the file</param>
        /// <param name="key">search key</param>
        /// <param name="info">stream info related to stream being appended</param>
        /// <param name="token">cancellation token for asynchronous operation</param>
        /// <returns>Task object that will be completed once stream data is written</returns>
        public void Write(Stream data, string key, StreamInfo info, CancellationToken token)
        {
            if (binaryStorageIndex.Contains(key))
            {
                throw new ArgumentException(nameof(key));
            }

            var newInfo = PreValidateStream(data, info);

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationOnDispose.Token);
            linkedTokenSource.Token.ThrowIfCancellationRequested();

            if (!linkedTokenSource.IsCancellationRequested)
            {
                ManualResetEvent result = null;
                lock (writeLock)
                {
                    if (!linkedTokenSource.IsCancellationRequested)
                    {
                        result = InternalWrite(data, key, newInfo, linkedTokenSource.Token);
                    }
                }

                if (!linkedTokenSource.IsCancellationRequested)
                {
                    result?.WaitOne();
                }
            }
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
            cancellationOnDispose.Cancel();
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

        private ManualResetEvent InternalWrite(Stream data, string key, StreamInfo info, CancellationToken token)
        {
            var streamMetadata = new StreamMetadata()
            {
                Offset = binaryStorageIndex.TotalLength,
                Key = key,
                Length = info.Length.Value,
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

        private bool NeedCompression(Stream data, StreamInfo info)
        {
            var expectedLength = info.Length ?? (data.CanSeek ? data.Length : 0);
            return info.IsCompressed && (configuration.CompressionThreshold <= 0 || expectedLength > configuration.CompressionThreshold);
        }

        private ManualResetEvent WriteToBuffer(Stream data, CancellationToken token, StreamMetadata streamMetadata)
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

                return GetWaitingEvent();
            }
        }

        private StreamInfo PreValidateStream(Stream data, StreamInfo info)
        {
            try
            {
                var length = data.Length;

                if (info.Length.HasValue && info.Length != length)
                {
                    throw new ArgumentException(nameof(info.Length));
                }

                return new StreamInfo()
                {
                    Length = length,
                    Hash = info.Hash,
                    IsCompressed = info.IsCompressed
                };
            }
            catch (NotSupportedException)
            {
                throw new IOException(NotSupportedLength);
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

        private ManualResetEvent GetWaitingEvent()
        {
            var result = new ManualResetEvent(false);
            waitingList.Enqueue(new Tuple<long, ManualResetEvent>(pendingCounter, result));
            if (Interlocked.Read(ref processedCounter) >= pendingCounter)
            {
                result.Set();
                return null;
            }

            return result;
        }

        private void OnCounterChanged(int processed)
        {
            Interlocked.Add(ref processedCounter, processed);
            Tuple<long, ManualResetEvent> item;
            while (waitingList.TryPeek(out item))
            {
                if (item.Item1 > processedCounter)
                {
                    break;
                }

                item.Item2.Set();
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