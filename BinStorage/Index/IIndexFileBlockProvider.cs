using System;

namespace BinStorage.Index
{
    internal interface IIndexFileBlockProvider : IDisposable
    {
        /// <summary>
        /// Total length of index file
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Loads block contents from index file
        /// </summary>
        /// <param name="data">Block object</param>
        /// <param name="offset">Offset of block in index file</param>
        /// <param name="length">Length of block in index file</param>
        /// <exception cref="IOException">not enough data in index file to fill the block</exception>
        void Read(ISerializableIndexData data, long offset, int length);

        /// <summary>
        /// Saves block contents to index file
        /// </summary>
        /// <param name="data">Block object</param>
        /// <param name="offset">Desired offset of block in index file, default in null, that means append to the end of file</param>
        /// <returns>offset of the written block in index file</returns>
        long Write(ISerializableIndexData data, long? offset = null);
    }
}