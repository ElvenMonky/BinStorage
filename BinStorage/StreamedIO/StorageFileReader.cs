using System.IO;
using BinStorage.Common;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides asynchronous chain of write requests to the file
    /// </summary>
    internal class StorageFileReader : IStorageFileReader
    {
        private readonly IStreamFactory streamFactory;

        /// <summary>
        /// Initializes new instance of <see cref="StorageFileAppender"/> class.
        /// </summary>
        /// <param name="streamFactory"><see cref="IStreamFactory"/> object used to create file streams</param>
        /// <exception cref="ArgumentNullException">factory is null</exception>
        public StorageFileReader(IStreamFactory streamFactory)
        {
            this.streamFactory = streamFactory.ThrowIfNull(nameof(streamFactory));
        }

        /// <summary>
        /// Provides access to stream from storage file
        /// </summary>
        /// <param name="data">stream related metadata</param>
        /// <returns>Stream object that can be used to read data from stream</returns>
        /// <exception cref="IOException">storage file could not be opened</exception>
        public Stream Get(StreamMetadata data)
        {
            return streamFactory.CreateStorageReadingStream(data);
        }
    }
}