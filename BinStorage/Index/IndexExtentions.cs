using System;

namespace BinStorage.Index
{
    internal static class IndexExtentions
    {
        /// <summary>
        /// Creates block and reads its contents from index file
        /// </summary>
        /// <param name="offset">Offset of block in index file</param>
        /// <param name="length">Length of block in index file</param>
        /// <returns>New block instance</returns>
        /// <exception cref="IOException">not enough data in index file to fill the block</exception>
        public static T Read<T>(this IIndexFileBlockProvider blockProvider, long offset, int length) where T : ISerializableIndexData, new()
        {
            var result = new T();
            blockProvider.Read(result, offset, length);
            return result;
        }

        /// <summary>
        /// Creates concrete implementation of <see cref="ISerializableIndexData"/> interface and reads its contents from given buffer
        /// </summary>
        /// <param name="offset">Offset of object within buffer</param>
        /// <returns>New <see cref="ISerializableIndexData"/> instance</returns>
        /// <exception cref="ArgumentException">buffer doesn't fit deserialization needs</exception>
        public static T Read<T>(this byte[] buffer, int offset) where T : ISerializableIndexData, new()
        {
            var result = new T();
            result.Deserialize(buffer, offset);
            return result;
        }

        /// <summary>
        /// Extracts byte buffer of specified size from given buffer
        /// </summary>
        /// <param name="offset">Offset of object within buffer</param>
        /// <param name="size">Size of uffer to read</param>
        /// <returns>extracted value</returns>
        /// <exception cref="ArgumentException">buffer doesn't fit deserialization needs</exception>
        public static byte[] ReadByteBuffer(this byte[] buffer, ref int offset, int size)
        {
            var result = new byte[size];
            Buffer.BlockCopy(buffer, offset, result, 0, size);
            offset += size;
            return result;
        }

        /// <summary>
        /// Writes byte buffer value to given buffer
        /// </summary>
        /// <param name="offset">Offset of object within buffer</param>
        /// <param name="value">value to be written</param>
        public static void WriteByteBuffer(this byte[] buffer, ref int offset, byte[] value)
        {
            Buffer.BlockCopy(value, 0, buffer, offset, value.Length);
            offset += value.Length;
        }

        /// <summary>
        /// Extracts long value from given buffer
        /// </summary>
        /// <param name="offset">Offset of object within buffer</param>
        /// <returns>extracted value</returns>
        /// <exception cref="ArgumentException">buffer doesn't fit deserialization needs</exception>
        public static long ReadLong(this byte[] buffer, ref int offset)
        {
            var result = BitConverter.ToInt64(buffer, offset);
            offset += sizeof(long);
            return result;
        }

        /// <summary>
        /// Writes long value to given buffer
        /// </summary>
        /// <param name="offset">Offset of object within buffer</param>
        /// <param name="value">value to be written</param>
        public static void WriteLong(this byte[] buffer, ref int offset, long value)
        {
            var temp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(temp, 0, buffer, offset, sizeof(long));
            offset += sizeof(long);
        }

        /// <summary>
        /// Extracts int value from given buffer
        /// </summary>
        /// <param name="offset">Offset of object within buffer</param>
        /// <returns>extracted value</returns>
        /// <exception cref="ArgumentException">buffer doesn't fit deserialization needs</exception>
        public static int ReadInt(this byte[] buffer, ref int offset)
        {
            var result = BitConverter.ToInt32(buffer, offset);
            offset += sizeof(int);
            return result;
        }

        /// <summary>
        /// Writes int value to given buffer
        /// </summary>
        /// <param name="offset">Offset of object within buffer</param>
        /// <param name="value">value to be written</param>
        public static void WriteInt(this byte[] buffer, ref int offset, int value)
        {
            var temp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(temp, 0, buffer, offset, sizeof(int));
            offset += sizeof(int);
        }
    }
}