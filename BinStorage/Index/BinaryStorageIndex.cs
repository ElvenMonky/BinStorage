using System;
using System.Collections.Generic;
using BinStorage.Common;

namespace BinStorage.Index
{
    internal class BinaryStorageIndex : IBinaryStorageIndex
    {
        private const int MaxIndexBlockLength = 0x10000000; //256MB
        private readonly IIndexFileBlockProvider blockProvider;
        private readonly IndexHeader header;

        /// <summary>
        /// Initializes new instance of <see cref="StorageFileReader"/> class.
        /// </summary>
        /// <param name="streamFactory"><see cref="IStreamFactory"/> object used to create file streams</param>
        /// <exception cref="ArgumentNullException">factory is null</exception>
        public BinaryStorageIndex(IIndexFileBlockProvider blockProvider)
        {
            this.blockProvider = blockProvider.ThrowIfNull(nameof(blockProvider));
            if (blockProvider.Length == 0)
            {
                header = new IndexHeader();
                blockProvider.Write(header, 0);
            }
            else
            {
                header = blockProvider.Read<IndexHeader>(0, IndexHeader.FullHeaderSize);
            }
        }

        /// <summary>
        /// Total length of storage file written
        /// </summary>
        public long TotalLength
        {
            get
            {
                return header.StorageWrittenLength;
            }
        }

        /// <summary>
        /// Tests if key is present in the index
        /// </summary>
        /// <param name="key">string key</param>
        /// <returns>'true' if key is present, 'false' otherwise</returns>
        /// <exception cref="System.ArgumentNullException">key is 'null'</exception>
        public bool Contains(string key)
        {
            return InternalGet(key) != null;
        }

        /// <summary>
        /// Retrieves <see cref="StreamMetadata"/> object associated with the given key
        /// </summary>
        /// <param name="key">String key</param>
        /// <returns>The stream metadata associated with the given key</returns>
        /// <exception cref="System.ArgumentNullException">key is 'null'</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The given key is not present</exception>
        public StreamMetadata Get(string key)
        {
            var result = InternalGet(key);
            if (result == null)
            {
                throw new KeyNotFoundException();
            }

            return result;
        }

        /// <summary>
        /// Adds metadata of written stream to index file
        /// </summary>
        /// <param name="streamMetadata">stream metadata to be added to index</param>
        /// <exception cref="System.ArgumentException">An element with the same key already exists</exception>
        public void Set(StreamMetadata streamMetadata)
        {
            streamMetadata.ThrowIfNull(nameof(streamMetadata));
            var key = streamMetadata.Key;
            if (Contains(key))
            {
                throw new ArgumentException(nameof(key));
            }

            var blockInfo = header.Get(key);
            var block = new IndexBlock();
            var oldLength = 0;
            if (blockInfo.Offset > 0)
            {
                if (blockInfo.Length + streamMetadata.SerializedLength < MaxIndexBlockLength)
                {
                    blockProvider.Read(block, blockInfo.Offset, blockInfo.Length);
                    oldLength = blockInfo.Length;
                }
                else
                {
                    block = new IndexBlock(blockInfo);
                }
            }
            block.Add(streamMetadata);

            var offset = blockProvider.Write(block);
            var length = block.SerializedLength;
            blockInfo = new BlockInfo() { Offset = offset, Length = length };

            header.Set(key, blockInfo);
            header.IndexWrittenLength += length - oldLength;
            header.StorageWrittenLength += streamMetadata.Length;
        }

        /// <summary>
        /// Skips given amount of invalid bytes for failed stream and shifts storage file length counter by appropriate value
        /// </summary>
        /// <param name="length">stream length to skip</param>
        public void Skip(long length)
        {
            header.StorageWrittenLength += length;
        }

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        /// <remarks>
        /// Note that FileStream itself is managed resource, that implements IDisposable pattern,
        /// so finalizer is not required in the class that uses it.
        /// </remarks>
        public void Dispose()
        {
            blockProvider.Write(header, 0);
            blockProvider.Dispose();
        }

        private StreamMetadata InternalGet(string key)
        {
            StreamMetadata result = null;
            var blockInfo = header.Get(key);

            while (blockInfo.Offset != 0)
            {
                var block = new IndexBlock();
                blockProvider.Read(block, blockInfo.Offset, blockInfo.Length);

                blockInfo = block.Get(key, ref result);

                if (result != null)
                {
                    return result;
                }
            }

            return result;
        }
    }
}