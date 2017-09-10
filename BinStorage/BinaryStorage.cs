using System.IO;
using System.Threading;
using BinStorage.Common;
using BinStorage.Index;
using BinStorage.StreamedIO;

namespace BinStorage
{
    public class BinaryStorage : IBinaryStorage
    {
        private readonly CancellationTokenSource cancellationOnDispose;
        private readonly IBinaryStorageFactory binaryStorageFactory;
        private readonly IStorageFileAppender storageFileAppender;
        private readonly IStorageFileReader storageFileReader;
        private readonly IBinaryStorageIndex binaryStorageIndex;

        /// <summary>
        /// Initializes new instance of <see cref="BinaryStorage"/> class.
        /// </summary>
        /// <param name="configuration">The configuration parameters for binary storage</param>
        public BinaryStorage(StorageConfiguration configuration)
        {
            binaryStorageFactory = new BinaryStorageFactory(configuration);

            cancellationOnDispose = new CancellationTokenSource();

            var blockProvider = binaryStorageFactory.CreateIndexFileBlockProvider();
            binaryStorageIndex = binaryStorageFactory.CreateBinaryStorageIndex(blockProvider);
            storageFileAppender = binaryStorageFactory.CreateStorageFileAppender(binaryStorageIndex);
            storageFileReader = binaryStorageFactory.CreateStorageFileReader();
        }

        /// <summary>
        /// Add data to the storage
        /// </summary>
        /// <param name="key">Unique identifier of the stream, cannot be null or empty</param>
        /// <param name="data">Non empty stream with data, cannot be null or empty </param>
        /// <param name="parameters">Optional parameters. Instead of null use StreamInfo.Empty</param>
        /// <exception cref="System.ArgumentException">
        /// An element with the same key already exists or
        /// provided hash or length does not match the data.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///  key is null, data is null, parameters is null
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///  I/O exception occurred during persisting data
        /// </exception>
        public void Add(string key, Stream data, StreamInfo parameters)
        {
            key.ThrowIfNull(nameof(key));
            data.ThrowIfNull(nameof(data));
            parameters.ThrowIfNull(nameof(parameters));

            storageFileAppender.Write(data, key, parameters, cancellationOnDispose.Token);
        }

        /// <summary>
        /// Get stream with data from the storage
        /// </summary>
        /// <param name="key">Unique identifier of the stream</param>
        /// <returns>Stream with data</returns>
        /// <exception cref="System.ArgumentNullException">
        ///  key is null.
        /// </exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        ///  key does not exist
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///  I/O exception occurred during read
        /// </exception>
        public Stream Get(string key)
        {
            var streamMetadata = binaryStorageIndex.Get(key);
            return storageFileReader.Get(streamMetadata);
        }

        /// <summary>
        /// Check if key is present in the storage
        /// </summary>
        /// <param name="key">Unique identifier of the stream</param>
        /// <returns>true if key is present and false otherwise</returns>
        public bool Contains(string key)
        {
            return binaryStorageIndex.Contains(key);
        }

        /// <summary>
        /// Disposes object
        /// </summary>
        public void Dispose()
        {
            cancellationOnDispose.Cancel();
            storageFileAppender.Dispose();
            binaryStorageIndex.Dispose();
        }
    }
}