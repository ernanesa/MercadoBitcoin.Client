using System.Threading;

namespace MercadoBitcoin.Client.Internal.RateLimiting;

internal sealed class RateLimiterMetrics
{
    private long _acquired;
    private long _failed;
    private long _queued;

    public void IncrementAcquired() => Interlocked.Increment(ref _acquired);
    public void IncrementFailed() => Interlocked.Increment(ref _failed);
    public void IncrementQueued() => Interlocked.Increment(ref _queued);

    public (long acquired, long failed, long queued) Snapshot()
    {
        return (
            Interlocked.Read(ref _acquired),
            Interlocked.Read(ref _failed),
            Interlocked.Read(ref _queued));
    }
}
