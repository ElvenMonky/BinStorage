using BinStorage.Index;
using BinStorage.StreamedIO;

namespace BinStorage
{
    /// <summary>
    /// Provides interface for factories, that create components required for proper binary storage operation
    /// </summary>
    internal interface IBinaryStorageFactory
    {
        /// <summary>
        /// Creates <see cref="IIndexFileBlockProvider"/> instance, used by <see cref="IBinaryStorageIndex"/> to access the index file.
        /// </summary>
        /// <returns>New <see cref="IIndexFileBlockProvider"/> instance</returns>
        IIndexFileBlockProvider CreateIndexFileBlockProvider();

        /// <summary>
        /// Creates <see cref="IBinaryStorageIndex"/> instance, responsible for providing record metadata by keys.
        /// </summary>
        /// <param name="blockProvider">The <see cref="IIndexFileBlockProvider"/> instance, used by <see cref="IBinaryStorageIndex"/> to access the index file</param>
        /// <returns>New <see cref="IBinaryStorageIndex"/> instance</returns>
        IBinaryStorageIndex CreateBinaryStorageIndex(IIndexFileBlockProvider blockProvider);

        /// <summary>
        /// Creates <see cref="IStorageFileAppender"/> instance, responsible for appending data to global storage file.
        /// </summary>
        /// <param name="binaryStorageIndex">reference to binary storage index structure</param>
        /// <returns>New <see cref="IStorageFileAppender"/> instance</returns>
        IStorageFileAppender CreateStorageFileAppender(IBinaryStorageIndex binaryStorageIndex);

        /// <summary>
        /// Creates <see cref="IStorageFileReader"/> instance, responsible for reading data from global storage file.
        /// </summary>
        /// <returns>New <see cref="IStorageFileReader"/> instance</returns>
        IStorageFileReader CreateStorageFileReader();
    }
}