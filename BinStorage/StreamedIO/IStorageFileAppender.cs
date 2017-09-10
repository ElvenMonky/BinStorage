using System;
using System.IO;
using System.Threading;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides interface for asynchronous writing to the files
    /// </summary>
    internal interface IStorageFileAppender: IDisposable
    {
        /// <summary>
        /// Appends stream to storage file, respecting previous append requests
        /// </summary>
        /// <param name="data">stream to be written to the file</param>
        /// <param name="key">search key</param>
        /// <param name="info">stream info related to stream being appended</param>
        /// <param name="token">cancellation token for asynchronous operation</param>
        /// <returns>Task object that will be completed once stream data is written</returns>
        void Write(Stream data, string key, StreamInfo info, CancellationToken token);
    }
}