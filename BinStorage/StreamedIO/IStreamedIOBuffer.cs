using System;
using System.IO;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides interface for IO buffers for transfering data between non-memory streams
    /// </summary>
    internal interface IStreamedIOBuffer: IDisposable
    {
        /// <summary>
        /// Reads data from buffer to specified stream
        /// </summary>
        /// <param name="data">stream to write data to</param>
        int ReadFromBuffer(Stream data);

        /// <summary>
        /// Write data from specified stream to a buffer
        /// </summary>
        /// <param name="data">stream to read data from</param>
        /// <returns>Number of bytes written, otherwise '0'</returns>
        int WriteToBuffer(Stream data);
    }
}