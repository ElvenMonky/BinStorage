using BinStorage.Common;
using BinStorage.Index;
using BinStorage.StreamedIO;

namespace BinStorage
{
    /// <summary>
    /// Creates components required for proper binary storage operation
    /// </summary>
    internal class BinaryStorageFactory : IBinaryStorageFactory
    {
        private readonly IStreamFactory streamFactory;
        private readonly StorageConfiguration configuration;

        /// <summary>
        /// Initializes new instance of <see cref="BinaryStorageFactory"/> class.
        /// </summary>
        /// <param name="configuration">The configuration parameters for binary storage components</param>
        /// <exception cref="System.ArgumentNullException">configuration is null</exception>
        public BinaryStorageFactory(StorageConfiguration configuration)
        {
            this.configuration = configuration.ThrowIfNull(nameof(configuration));
            streamFactory = new StreamFactory(configuration);
        }

        /// <summary>
        /// Creates <see cref="IStorageFileAppender"/> instance, responsible for appending data to global storage file.
        /// </summary>
        /// <param name="binaryStorageIndex">reference to binary storage index structure</param>
        /// <returns>New <see cref="IStorageFileAppender"/> instance</returns>
        public IStorageFileAppender CreateStorageFileAppender(IBinaryStorageIndex binaryStorageIndex)
        {
            return new StorageFileAppender(configuration, streamFactory, binaryStorageIndex);
        }

        /// <summary>
        /// Creates <see cref="IStorageFileReader"/> instance, responsible for reading data from global storage file.
        /// </summary>
        /// <returns>New <see cref="IStorageFileReader"/> instance</returns>
        public IStorageFileReader CreateStorageFileReader()
        {
            return new StorageFileReader(streamFactory);
        }

        /// <summary>
        /// Creates <see cref="IBinaryStorageIndex"/> instance, responsible for providing record metadata by keys.
        /// </summary>
        /// <param name="blockProvider">The <see cref="IIndexFileBlockProvider"/> instance, used by <see cref="IBinaryStorageIndex"/> to access the index file</param>
        /// <returns>New <see cref="IBinaryStorageIndex"/> instance</returns>
        public IBinaryStorageIndex CreateBinaryStorageIndex(IIndexFileBlockProvider blockProvider)
        {
            return new BinaryStorageIndex(blockProvider);
        }

        /// <summary>
        /// Creates <see cref="IIndexFileBlockProvider"/> instance, used by <see cref="IBinaryStorageIndex"/> to access the index file.
        /// </summary>
        /// <returns>New <see cref="IIndexFileBlockProvider"/> instance</returns>
        public IIndexFileBlockProvider CreateIndexFileBlockProvider()
        {
            return new IndexFileBlockProvider(streamFactory);
        }
    }
}