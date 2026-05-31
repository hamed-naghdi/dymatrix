using System.Threading.RateLimiting;
using DyMatrix.Domain.Exceptions;

namespace DyMatrix.Infrastructure.Services;

public sealed class RateLimiterService : IDisposable
{
    private readonly SlidingWindowRateLimiter _rateLimiter;

    public RateLimiterService()
    {
        _rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,           // 10-second resolution
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0                   // reject immediately, no queuing
        });
    }
    
    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
            throw new RateLimitExceededException(10);
    }

    public void Dispose() => _rateLimiter.Dispose();
}