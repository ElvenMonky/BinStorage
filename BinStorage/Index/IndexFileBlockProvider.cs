using System.IO;
using BinStorage.Common;
using BinStorage.StreamedIO;

namespace BinStorage.Index
{
    /// <summary>
    /// Provides reading/writing capabilities for blocks in the index file
    /// </summary>
    internal class IndexFileBlockProvider : IIndexFileBlockProvider
    {
        private const string IndexBlockReadErrorMessage = "Unable to read enough data from index file.";

        private readonly object fileStreamLock;
        private readonly Stream fileStream;

        /// <summary>
        /// Initializes new instance of <see cref="StorageFileReader"/> class.
        /// </summary>
        /// <param name="streamFactory"><see cref="IStreamFactory"/> object used to create file streams</param>
        /// <exception cref="ArgumentNullException">factory is null</exception>
        public IndexFileBlockProvider(IStreamFactory streamFactory)
        {
            streamFactory.ThrowIfNull(nameof(streamFactory));
            fileStream = streamFactory.CreateIndexStream();
            fileStreamLock = new object();
        }

        /// <summary>
        /// Total length of index file
        /// </summary>
        public long Length
        {
            get
            {
                lock(fileStreamLock)
                {
                    return fileStream.Length;
                }
            }
        }

        /// <summary>
        /// Loads block contents from index file
        /// </summary>
        /// <param name="data">Block object</param>
        /// <param name="offset">Offset of block in index file</param>
        /// <param name="length">Length of block in index file</param>
        /// <exception cref="IOException">not enough data in index file to fill the block</exception>
        public void Read(ISerializableIndexData data, long offset, int length)
        {
            var buffer = new byte[length];

            lock (fileStreamLock)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                var bytesRead = fileStream.Read(buffer, 0, length);
                if (bytesRead != length)
                {
                    throw new IOException(IndexBlockReadErrorMessage);
                }
            }

            data.Deserialize(buffer, 0);
        }

        /// <summary>
        /// Saves block contents to index file
        /// </summary>
        /// <param name="data">Block object</param>
        /// <param name="offset">Desired offset of block in index file, default in null, that means append to the end of file</param>
        /// <returns>offset of the written block in index file</returns>
        public long Write(ISerializableIndexData data, long? offset = null)
        {
            var length = data.SerializedLength;
            var buffer = new byte[length];

            data.Serialize(buffer, 0);

            lock (fileStreamLock)
            {
                var newOffset = fileStream.Seek(offset ?? fileStream.Length, SeekOrigin.Begin);
                fileStream.Write(buffer, 0, length);
                fileStream.Flush();
                return newOffset;
            }
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
            fileStream.Dispose();
        }
    }
}