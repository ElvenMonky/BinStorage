namespace BinStorage.Index
{
    /// <summary>
    /// Provides interface for segment blocks in index file
    /// </summary>
    internal interface IIndexBlock: ISerializableIndexData
    {
        /// <summary>
        /// Searches for given key in block. If found provides found metadata through "data" reference parameter.
        /// Otherwice returns address of next related block. If no further blocks, throws <see cref="KeyNotFoundException"/>
        /// </summary>
        /// <param name="key">search key</param>
        /// <param name="data">If key is found stored metadata will be returned here</param>
        /// <returns>Address of next block to search for key in index file, <c>0</c> if key found in current block</returns>
        /// <exception cref="KeyNotFoundException">Key is not found in block and there is no further block to search in</exception>
        BlockInfo Get(string key, ref StreamMetadata data);
    }
}