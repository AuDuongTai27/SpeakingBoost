// File: Services/Background/BackgroundQueue.cs
using System.Threading.Channels;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace SpeakingBoost.Services.Background
{
    public class BackgroundQueue
    {
        private readonly Channel<int> _queue;
        private int _queuedCount;
        public int Capacity { get; }
        public int QueuedCount => Volatile.Read(ref _queuedCount);

        public BackgroundQueue(IConfiguration configuration)
        {
            Capacity = configuration.GetValue<int?>("StudentGrading:QueueCapacity") ?? 200;
            _queue = Channel.CreateBounded<int>(new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = false,
                SingleWriter = false
            });
        }

        public bool TryQueueBackgroundWorkItem(int submissionId)
        {
            var written = _queue.Writer.TryWrite(submissionId);
            if (written)
            {
                Interlocked.Increment(ref _queuedCount);
            }

            return written;
        }

        public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            var item = await _queue.Reader.ReadAsync(cancellationToken);
            Interlocked.Decrement(ref _queuedCount);
            return item;
        }
    }
}
