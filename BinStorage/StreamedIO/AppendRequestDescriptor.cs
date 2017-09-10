using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BinStorage.Common;

namespace BinStorage.StreamedIO
{
    internal class AppendRequestDescriptor
    {
        public AppendRequestDescriptor(Stream data, StreamMetadata metadata, CancellationToken token)
        {
            Data = data.ThrowIfNull(nameof(data));
            Metadata = metadata.ThrowIfNull(nameof(metadata));
            Token = token.ThrowIfNull(nameof(token));
            TaskSource = new TaskCompletionSource<object>();
        }

        public Stream Data { get; }
        public StreamMetadata Metadata { get; }
        public CancellationToken Token { get; }
        public TaskCompletionSource<object> TaskSource { get; }
        public long? startCounter { get; set; }
        public long? finishCounter { get; set; }
    }
}