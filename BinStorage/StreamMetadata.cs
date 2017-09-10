using System;
using System.Text;
using BinStorage.Index;

namespace BinStorage
{
    /// <summary>
    /// Represents stream metadata stored in the index file 
    /// </summary>
    public class StreamMetadata: ISerializableIndexData, IComparable<StreamMetadata>
    {
        private const int MD5HashSize = 16;

        /// <summary>
        /// Default instance of metadata
        /// </summary>
        public static StreamMetadata Empty => new StreamMetadata();

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
                return 2 * sizeof(long) + MD5HashSize + sizeof(int) + sizeof(char) * Key.Length;
            }
        }

        /// <summary>
        /// Gets if stream is compressed with gzip
        /// </summary>
        /// <remarks>
        /// migth be upgraded later into compression method code + compression level
        /// but for now gzip level 1 seems optimal in terms of performance.
        /// </remarks>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Offset of stream in storage file
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Length of stream in storage file
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Stream md5 default hash
        /// </summary>
        public byte[] Hash { get; set; }

        /// <summary>byte
        /// Stream search key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Deserializes data from given buffer starting at specified offset
        /// </summary>
        /// <param name="buffer">buffer with serialized data</param>
        /// <param name="offset">offset within the buffer where object data starts</param>
        public void Deserialize(byte[] buffer, int offset)
        {
            Offset = buffer.ReadLong(ref offset);
            Length = buffer.ReadLong(ref offset);
            Hash = buffer.ReadByteBuffer(ref offset, MD5HashSize);
            var keyLength = buffer.ReadInt(ref offset);

            Key = Encoding.Unicode.GetString(buffer, offset, sizeof(char) * keyLength);

            IsCompressed = Length < 0;
            Length = Math.Abs(Length);
        }

        /// <summary>
        /// Serializes data into provided buffer starting at specified offset
        /// </summary>
        /// <param name="buffer">buffer to write serialized data to</param>
        /// <param name="offset">offset within the buffer to start writing from</param>
        public void Serialize(byte[] buffer, int offset)
        {
            buffer.WriteLong(ref offset, Offset);
            buffer.WriteLong(ref offset, Length * (IsCompressed ? -1 : 1));
            buffer.WriteByteBuffer(ref offset, Hash);
            buffer.WriteInt(ref offset, Key.Length);

            Buffer.BlockCopy(Key.ToCharArray(), 0, buffer, offset, sizeof(char) * Key.Length);
        }

        public int CompareTo(StreamMetadata other)
        {
            return Key.CompareTo(other.Key);
        }
    }
}