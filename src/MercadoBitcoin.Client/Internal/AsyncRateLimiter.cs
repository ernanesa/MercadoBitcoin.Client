using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal
{
    /// <summary>
    /// Rate limiter assíncrono e thread-safe, baseado em Channel e Task.WhenEach, configurável por requisições por segundo.
    /// </summary>
    public class AsyncRateLimiter : IDisposable
    {
        private readonly Channel<DateTime> _channel;
        private readonly int _requestsPerSecond;
        private readonly Timer _timer;
        private readonly ConcurrentQueue<TaskCompletionSource<bool>> _waiters = new();
        private bool _disposed;

        public AsyncRateLimiter(int requestsPerSecond)
        {
            if (requestsPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(requestsPerSecond));
            _requestsPerSecond = requestsPerSecond;
            _channel = Channel.CreateUnbounded<DateTime>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
            _timer = new Timer(ReleaseSlots, null, 0, 1000 / _requestsPerSecond);
        }

        private void ReleaseSlots(object? state)
        {
            if (_disposed) return;
            if (_waiters.TryDequeue(out var tcs))
            {
                tcs.TrySetResult(true);
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AsyncRateLimiter));
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(tcs);
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _timer.Dispose();
            while (_waiters.TryDequeue(out var tcs))
                tcs.TrySetCanceled();
        }
    }
}
