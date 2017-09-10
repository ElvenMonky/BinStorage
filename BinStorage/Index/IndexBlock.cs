using System;
using System.Collections.Generic;
using System.Linq;
using BinStorage.Common;

namespace BinStorage.Index
{
    internal class IndexBlock : IIndexBlock
    {
        private IList<StreamMetadata> payload;
        private BlockInfo nextBlock;

        /// <summary>
        /// Initializes new empty instance of <see cref="IndexBlock"/> class.
        /// </summary>
        public IndexBlock()
        {
            payload = new List<StreamMetadata>();
            nextBlock = BlockInfo.Empty;
        }

        /// <summary>
        /// Initializes new instance of <see cref="IndexBlock"/> class by given next block address
        /// </summary>
        /// <param name="nextBlock">Tha address of the next related block in index file</param>
        public IndexBlock(BlockInfo nextBlock)
        {
            payload = new List<StreamMetadata>();
            this.nextBlock = nextBlock;
        }

        /// <summary>
        /// Initializes new instance of <see cref="IndexBlock"/> class by given collection of stream metadata and next block address
        /// </summary>
        /// <param name="payload">The <see cref="StreamMetadata"/> collection</param>
        /// <param name="nextBlock">Tha address of the next related block in index file</param>
        public IndexBlock(IEnumerable<StreamMetadata> payload, BlockInfo nextBlock)
        {
            this.payload = payload.OrderBy(x => x.Key).ToList();
            this.nextBlock = nextBlock;
        }

        /// <summary>
        /// Gets size of serialized representation of the object 
        /// </summary>
        /// <remarks>
        /// Value should be known before serialization and after deserialization
        /// </remarks>
        public int SerializedLength
        {
            get
            {
                return BlockInfo.Size + sizeof(int) + payload.Sum(x => x.SerializedLength);
            }
        }

        /// <summary>
        /// Searches for given key in block. If found provides found metadata through "data" reference parameter.
        /// Otherwice returns address of next related block. If no further blocks, throws <see cref="KeyNotFoundException"/>
        /// </summary>
        /// <param name="key">search key</param>
        /// <param name="data">If key is found stored metadata will be returned here</param>
        /// <returns>
        /// Address of next block to search for key in index file,
        /// <c>0</c> if key found in current block or
        /// key is not found in block and there is no further block to search in
        /// </returns>
        public BlockInfo Get(string key, ref StreamMetadata data)
        {
            key.ThrowIfNull(nameof(key));

            var index = payload.BinarySearch(key, x => x.Key);
            if (index >= 0)
            {
                data = payload[index];
                return BlockInfo.Empty;
            }

            return nextBlock;
        }

        /// <summary>
        /// Searches given metatdata in block and inserts at desired position if not found
        /// </summary>
        /// <param name="data">The <see cref="StreamMetadata"/> object to be added</param>
        public void Add(StreamMetadata data)
        {
            data.ThrowIfNull(nameof(data));

            var index = payload.BinarySearch(data);
            if (index >= 0)
            {
                throw new ArgumentException(nameof(data.Key));
            }

            index = ~index;
            payload.Insert(index, data);
        }

        /// <summary>
        /// Deserializes data from given buffer starting at specified offset
        /// </summary>
        /// <param name="buffer">buffer with serialized data</param>
        /// <param name="offset">offset within the buffer where object data starts</param>
        public void Deserialize(byte[] buffer, int offset)
        {
            nextBlock.Offset = buffer.ReadLong(ref offset);
            nextBlock.Length = buffer.ReadInt(ref offset);
            var payloadCount = buffer.ReadInt(ref offset);
            while (payloadCount > 0)
            {
                var data = buffer.Read<StreamMetadata>(offset);
                offset += data.SerializedLength;

                payload.Add(data);
                --payloadCount;
            }
        }

        /// <summary>
        /// Serializes data into provided buffer starting at specified offset
        /// </summary>
        /// <param name="buffer">buffer to write serialized data to</param>
        /// <param name="offset">offset within the buffer to start writing from</param>
        public void Serialize(byte[] buffer, int offset)
        {
            buffer.WriteLong(ref offset, nextBlock.Offset);
            buffer.WriteInt(ref offset, nextBlock.Length);
            buffer.WriteInt(ref offset, payload.Count);
            foreach (var data in payload)
            {
                data.Serialize(buffer, offset);
                offset += data.SerializedLength;
            }
        }
    }
}