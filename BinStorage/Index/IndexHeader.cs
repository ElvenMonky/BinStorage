using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BinStorage.Index
{
    internal class IndexHeader : ISerializableIndexData
    {
        private const int HashTableSize = 0xFFFE; //(65536 - 1)*ulong = 512KB

        public static readonly int FullHeaderSize = BlockInfo.Size * HashTableSize + 2 * sizeof(long);

        private readonly long[] blockOffsetHash;
        private readonly int[] blockLengthHash;

        /// <summary>
        /// Initializes new instance of <see cref="IndexHeader"/> class.
        /// </summary>
        /// <param name="streamFactory"><see cref="IStreamFactory"/> object used to create file streams</param>
        /// <exception cref="ArgumentNullException">factory is null</exception>
        public IndexHeader()
        {
            blockOffsetHash = new long[HashTableSize];
            blockLengthHash = new int[HashTableSize];
        }

        /// <summary>
        /// Gets size of serialized representation of the object 
        /// </summary>
        public int SerializedLength
        {
            get
            {
                return FullHeaderSize;
            }
        }

        /// <summary>
        /// Total length of all data written to the storage file
        /// </summary>
        public long StorageWrittenLength { get; set; }

        /// <summary>
        /// Total length of all data written to the index file
        /// </summary>
        public long IndexWrittenLength { get; set; }

        public BlockInfo Get(string key)
        {
            var hash = GetHash(key);
            return new BlockInfo()
            {
                Offset = blockOffsetHash[hash],
                Length = blockLengthHash[hash]
            };
        }

        public void Set(string key, BlockInfo value)
        {
            var hash = GetHash(key);
            blockOffsetHash[hash] = value.Offset;
            blockLengthHash[hash] = value.Length;
        }

        public void Deserialize(byte[] buffer, int offset)
        {
            StorageWrittenLength = buffer.ReadLong(ref offset);
            IndexWrittenLength = buffer.ReadLong(ref offset);
            Buffer.BlockCopy(buffer, offset, blockOffsetHash, 0, HashTableSize * sizeof(long));
            offset += HashTableSize * sizeof(long);
            Buffer.BlockCopy(buffer, offset, blockLengthHash, 0, HashTableSize * sizeof(int));
        }

        public void Serialize(byte[] buffer, int offset)
        {
            buffer.WriteLong(ref offset, StorageWrittenLength);
            buffer.WriteLong(ref offset, IndexWrittenLength);
            Buffer.BlockCopy(blockOffsetHash, 0, buffer, offset, HashTableSize * sizeof(long));
            offset += HashTableSize * sizeof(long);
            Buffer.BlockCopy(blockLengthHash, 0, buffer, offset, HashTableSize * sizeof(int));
        }

        private int GetHash(string key)
        {
            using (var md5 = MD5.Create())
            {
                var buffer = Encoding.Unicode.GetBytes(key);
                var md5Hash = md5.TransformFinalBlock(buffer, 0, buffer.Length);
                var hash = md5Hash.Aggregate(397, (s, c) => (s * 397) ^ c);
                return Math.Abs(hash % HashTableSize);
            }
        }
    }
}