using System;

namespace BinStorage.Index
{
    /// <summary>
    /// Provides interface for classes representing index structure of binary storage
    /// </summary>
    public interface IBinaryStorageIndex : IDisposable
    {
        /// <summary>
        /// Total length of storage file written
        /// </summary>
        long TotalLength { get; }

        /// <summary>
        /// Tests if key is present in the index
        /// </summary>
        /// <param name="key">string key</param>
        /// <returns>'true' if key is present, 'false' otherwise</returns>
        /// <exception cref="System.ArgumentNullException">key is 'null'</exception>
        bool Contains(string key);

        /// <summary>
        /// Retrieves <see cref="StreamMetadata"/> object associated with the given key
        /// </summary>
        /// <param name="key">String key</param>
        /// <returns>The stream metadata associated with the given key</returns>
        /// <exception cref="System.ArgumentNullException">key is 'null'</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The given key is not present</exception>
        StreamMetadata Get(string key);

        /// <summary>
        /// Adds metadata of written stream to index file
        /// </summary>
        /// <param name="streamMetadata">stream metadata to be added to index</param>
        /// <exception cref="System.ArgumentException">An element with the same key already exists</exception>
        void Set(StreamMetadata streamMetadata);

        /// <summary>
        /// Skips given amount of invalid bytes for failed stream and shifts storage file length counter by appropriate value
        /// </summary>
        /// <param name="length">stream length to skip</param>
        void Skip(long length);
    }
}