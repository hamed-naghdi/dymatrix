namespace DyMatrix.Domain.Exceptions;

public sealed class RateLimitExceededException : Exception
{
    public int LimitPerMinute { get; }

    public RateLimitExceededException(int limitPerMinute)
        : base($"Rate limit of {limitPerMinute} messages per minute has been exceeded.")
    {
        LimitPerMinute = limitPerMinute;
    }
}