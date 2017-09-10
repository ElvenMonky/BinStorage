using System;
using System.IO;
using System.IO.Compression;
using BinStorage.Common;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides interface for factories, that create components required for proper binary storage operation
    /// </summary>
    internal class StreamFactory : IStreamFactory
    {
        private static readonly string storageFileName = "storage.bin";
        private static readonly string indexFileName = "index.bin";

        private StorageConfiguration configuration;

        /// <summary>
        /// Initializes new instance of <see cref="StreamFactory"/> class.
        /// </summary>
        /// <param name="configuration">The configuration parameters for binary storage components</param>
        /// <exception cref="System.ArgumentNullException">configuration is null, WorkingFolder is null</exception>
        public StreamFactory(StorageConfiguration configuration)
        {
            this.configuration = configuration.ThrowIfNull(nameof(configuration));
            configuration.WorkingFolder.ThrowIfNull(nameof(configuration.WorkingFolder));
        }

        /// <summary>
        /// Creates <see cref="Stream"/> instance, responsible for writing streamed data to main storage file.
        /// </summary>
        /// <returns>New <see cref="Stream"/> instance</returns>
        public Stream CreateStorageAppendingStream()
        {
            return CreateFileStream(storageFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        }

        /// <summary>
        /// Creates <see cref="Stream"/> instance, responsible for reading streamed data from main storage file.
        /// </summary>
        /// <param name="streamMetadata">Represent part of the storage file to be read</param>
        /// <returns>New <see cref="Stream"/> instance</returns>
        public Stream CreateStorageReadingStream(StreamMetadata streamMetadata)
        {
            var result = CreateFileStream(storageFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            result = new BoundedReadonlyStream(result, streamMetadata.Offset, streamMetadata.Length);
            return streamMetadata.IsCompressed ? new GZipStream(result, CompressionMode.Decompress) : result;
        }

        /// <summary>
        /// Creates <see cref="Stream"/> instance, responsible for synchronizing index file.
        /// </summary>
        /// <returns>New <see cref="Stream"/> instance</returns>
        public Stream CreateIndexStream()
        {
            return CreateFileStream(indexFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>
        /// Creates <see cref="IStreamedIOBuffer"/> instance, used to transfer data between non-memory streams.
        /// </summary>
        /// <returns>New <see cref="IStreamedIOBuffer"/> instance</returns>
        public IStreamedIOBuffer CreateStreamedIOBuffer()
        {
            return new CyclicBuffer();
        }

        /// <summary>
        /// Creates <see cref="FileStream"/> instance
        /// </summary>
        /// <param name="fileName">Name of the file used to store data</param>
        /// <returns>New <see cref="FileStream"/> instance</returns>
        /// <exception cref="ArgumentNullException">fileName is null</exception>
        /// <exception cref="IOException">
        /// All access and formatting exceptions that might be thrown by 'File.Open'
        /// are wrapped into IOException to meet the requirements
        /// </exception>
        private Stream CreateFileStream(string fileName, FileMode mode, FileAccess access, FileShare share)
        {
            fileName.ThrowIfNull(nameof(fileName));

            try
            {
                var fullFileName = Path.Combine(configuration.WorkingFolder, fileName);
                var fileInfo = new FileInfo(fullFileName);

                return fileInfo.Open(mode, access, share);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }
    }
}