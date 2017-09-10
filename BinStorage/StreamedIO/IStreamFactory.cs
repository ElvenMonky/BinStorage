using System.IO;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides interface for factories, that create components required for IO operations
    /// </summary>
    internal interface IStreamFactory
    {
        /// <summary>
        /// Creates <see cref="Stream"/> instance, responsible for writing streamed data to main storage file.
        /// </summary>
        /// <returns>New <see cref="Stream"/> instance</returns>
        Stream CreateStorageAppendingStream();

        /// <summary>
        /// Creates <see cref="Stream"/> instance, responsible for reading streamed data from main storage file.
        /// </summary>
        /// <param name="streamMetadata">Represent part of the storage file to be read</param>
        /// <returns>New <see cref="Stream"/> instance</returns>
        Stream CreateStorageReadingStream(StreamMetadata streamMetadata);

        /// <summary>
        /// Creates <see cref="Stream"/> instance, responsible for synchronizing index file.
        /// </summary>
        /// <returns>New <see cref="Stream"/> instance</returns>
        Stream CreateIndexStream();

        /// <summary>
        /// Creates <see cref="IStreamedIOBuffer"/> instance, used to transfer data between non-memory streams.
        /// </summary>
        /// <returns>New <see cref="IStreamedIOBuffer"/> instance</returns>
        IStreamedIOBuffer CreateStreamedIOBuffer();
    }
}