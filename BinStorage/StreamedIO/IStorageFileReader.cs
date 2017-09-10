using System.IO;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides interface for accessing streams in storage
    /// </summary>
    internal interface IStorageFileReader
    {
        /// <summary>
        /// Provides access to stream from storage file
        /// </summary>
        /// <param name="data">stream related metadata</param>
        /// <returns>Stream object that can be used to read data from stream</returns>
        /// <exception cref="IOException">storage file could not be opened</exception>
        Stream Get(StreamMetadata data);
    }
}