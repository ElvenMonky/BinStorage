using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BinStorage.Common;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides asynchronous chain of write requests to the file
    /// </summary>
    internal class StorageFileAppender : IStorageFileAppender
    {
        private const string NotSupportedLength = "Incoming stream should provide length";

        private readonly CancellationTokenSource cancellationOnDispose;
        private readonly StorageConfiguration configuration;

        private readonly StorageFileAppendingQueue storageFileAppendingQueue;
        private readonly object writeLock;

        /// <summary>
        /// Initializes new instance of <see cref="StorageFileAppender"/> class.
        /// </summary>
        /// <param name="configuration">The configuration parameters for binary storage components</param>
        /// <param name="storageFileAppendingQueue"><see cref="StorageFileAppendingQueue"/> object used to create file streams</param>
        /// <exception cref="ArgumentNullException">configuration is null, storageFileAppendingQueue is null</exception>
        public StorageFileAppender(StorageConfiguration configuration, StorageFileAppendingQueue storageFileAppendingQueue)
        {
            this.configuration = configuration.ThrowIfNull(nameof(configuration));
            this.storageFileAppendingQueue = storageFileAppendingQueue.ThrowIfNull(nameof(storageFileAppendingQueue));

            cancellationOnDispose = new CancellationTokenSource();
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
            var length = PreValidateStreamLength(data, info);
            var streamMetadata = new StreamMetadata()
            {
                Key = key,
                Length = length,
                Hash = info.Hash,
                IsCompressed = NeedCompression(data, info)
            };

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationOnDispose.Token);
            linkedTokenSource.Token.ThrowIfCancellationRequested();

            Task result = null;
            lock (writeLock)
            {
                if (!linkedTokenSource.IsCancellationRequested)
                {
                    result = storageFileAppendingQueue.Add(data, streamMetadata, linkedTokenSource.Token);
                }
            }

            if (!linkedTokenSource.IsCancellationRequested)
            {
                try
                {
                    result?.Wait();
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
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
            storageFileAppendingQueue.Dispose();
        }

        private bool NeedCompression(Stream data, StreamInfo info)
        {
            var expectedLength = info.Length ?? (data.CanSeek ? data.Length : 0);
            return info.IsCompressed && (configuration.CompressionThreshold <= 0 || expectedLength > configuration.CompressionThreshold);
        }

        private long PreValidateStreamLength(Stream data, StreamInfo info)
        {
            try
            {
                var length = data.Length;

                if (info.Length.HasValue && info.Length != length)
                {
                    throw new ArgumentException(nameof(info.Length));
                }

                return length;
            }
            catch (NotSupportedException)
            {
                throw new IOException(NotSupportedLength);
            }
        }
    }
}