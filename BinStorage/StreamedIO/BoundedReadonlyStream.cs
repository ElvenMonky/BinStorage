using System;
using System.IO;
using BinStorage.Common;

namespace BinStorage.StreamedIO
{
    /// <summary>
    /// Provides readonly wrapper around other stream bounded by specified offset and length
    /// </summary>
    internal class BoundedReadonlyStream : Stream
    {
        private const string ReadOrSeekNotSupportedMessage = "Read and Seek operations must be supported by internal stream";

        private readonly Stream innerStream;
        private readonly long offset;
        private readonly long length;

        /// <summary>
        /// Initializes new instance of <see cref="BoundedReadonlyStream"/> class.
        /// </summary>
        /// <param name="innerStream"><see cref="Stream"/> object used to create file streams</param>
        /// <exception cref="ArgumentNullException">innerStream is null</exception>
        public BoundedReadonlyStream(Stream innerStream, long offset, long length)
        {
            this.innerStream = innerStream.ThrowIfNull(nameof(innerStream));
            this.offset = offset.ThrowIfNegative(nameof(offset));
            this.length = length.ThrowIfNegative(nameof(length));

            if (!innerStream.CanRead || !innerStream.CanSeek)
            {
                throw new ArgumentException(ReadOrSeekNotSupportedMessage);
            }

            var fileLength = innerStream.Length;
            if (fileLength < offset + length)
            {
                var isReallyShort = fileLength < offset;
                throw new ArgumentOutOfRangeException(isReallyShort ? nameof(offset) : nameof(length));
            }

            innerStream.Seek(offset, SeekOrigin.Begin);
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return length;
            }
        }

        public override long Position
        {
            get
            {
                return innerStream.Position - offset;
            }

            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, length - Position);
            if (count == 0)
            {
                return 0;
            }

            return innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var realOffset = this.offset + offset;
            if (origin == SeekOrigin.Current)
            {
                realOffset = innerStream.Position + offset;
            }
            else if (origin == SeekOrigin.End)
            {
                realOffset = this.offset + length + offset;
            }

            realOffset.ThrowIfOutOfRange(this.offset, this.offset + length - 1, nameof(offset));
            return innerStream.Seek(realOffset, SeekOrigin.Begin) - this.offset;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            innerStream.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}