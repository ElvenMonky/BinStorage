using System;
using System.IO;
using System.Threading;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Implements cyclic buffer for simultanious reading/writing of streams
    /// </summary>
    internal class CyclicBuffer : IStreamedIOBuffer
    {
        private const int MinBlockSizeThreshold = 0x0400; //1KB
        private const int DefaultBufferBlockSize = 0x4000;//16KB
        private const int MaxBlockSizeThreshold = 0x04000000; //64MB
        private const int BlockCount = 16;

        private readonly byte[] buffer;
        private readonly int blockSize;
        private readonly int bufferSize;

        private readonly object bufferLock;
        private readonly AutoResetEvent canRead;
        private readonly AutoResetEvent canWrite;
        private bool isDisposing;

        private int readOffset;
        private int writeOffset;

        /// <summary>
        /// Initializes new instance of <see cref="CyclicBuffer"/> class.
        /// </summary>
        /// <param name="blockSize">desired size for internal blocks, default is 16KB</param>
        public CyclicBuffer(int blockSize = DefaultBufferBlockSize)
        {
            this.blockSize = Math.Min(Math.Max(blockSize, MinBlockSizeThreshold), MaxBlockSizeThreshold);
            bufferSize = BlockCount * blockSize;
            buffer = new byte[bufferSize];
            bufferLock = new object();
            canRead = new AutoResetEvent(false);
            canWrite = new AutoResetEvent(true);
            isDisposing = false;
            readOffset = 0;
            writeOffset = 0;
        }

        /// <summary>
        /// Write data from specified stream to a buffer
        /// </summary>
        /// <param name="data">stream to read data from</param>
        /// <returns>Number of bytes written, otherwise '0'</returns>
        public int WriteToBuffer(Stream data)
        {
            if (isDisposing) return 0;
            canWrite.WaitOne();
            if (isDisposing) return 0;

            var sizeToWrite = GetNextSize(true);

            try
            {
                var result = data.Read(buffer, writeOffset, sizeToWrite);
                if (result > 0)
                {
                    UpdateStateAfterWrite(result);
                }
                else
                {
                    canWrite.Set();
                }

                return result;
            }
            catch
            {
                canWrite.Set();
                throw;
            }
        }

        /// <summary>
        /// Read data from buffer to specified stream
        /// </summary>
        /// <param name="data">stream to write data to</param>
        public int ReadFromBuffer(Stream data)
        {
            if (isDisposing) return 0;
            canRead.WaitOne();
            if (isDisposing) return 0;

            var sizeToRead = GetNextSize(false);

            try
            {
                data.Write(buffer, readOffset, sizeToRead);
                data.Flush();
                UpdateStateAfterRead(sizeToRead);

                return sizeToRead;
            }
            catch
            {
                canRead.Set();
                throw;
            }
        }

        /// <summary>
        /// Disposes buffer preventing further read/write operations
        /// </summary>
        public void Dispose()
        {
            isDisposing = true;
            canWrite.Set();
            canRead.Set();
        }

        private int GetNextSize(bool write)
        {
            var result = blockSize;
            lock (bufferLock)
            {
                var startOffset = write ? writeOffset : readOffset;
                var endOffset = write ? readOffset : writeOffset;
                result = Math.Min(result, bufferSize - startOffset);
                result = Math.Min(result, (bufferSize + endOffset - startOffset - 1) % bufferSize + 1);
            }

            return result;
        }

        private void UpdateStateAfterWrite(int bytesProcessed)
        {
            lock (bufferLock)
            {
                canRead.Set();
                writeOffset = (writeOffset + bytesProcessed) % bufferSize;
                if (writeOffset != readOffset)
                {
                    canWrite.Set();
                }
            }
        }

        private void UpdateStateAfterRead(int bytesProcessed)
        {
            lock (bufferLock)
            {
                canWrite.Set();
                readOffset = (readOffset + bytesProcessed) % bufferSize;
                if (writeOffset != readOffset)
                {
                    canRead.Set();
                }
            }
        }
    }
}