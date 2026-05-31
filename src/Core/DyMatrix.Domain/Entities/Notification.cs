using DyMatrix.Domain.Enums;

namespace DyMatrix.Domain.Entities;

public sealed class Notification
{
    private Notification()
    {
        // ORM
    }
    
    public static Notification Create(
        string title,
        string message,
        NotificationLevel level,
        TimeProvider timeProvider,
        string? source = null)
    {
        // Avoided using value objects with DDD pattern for such a small test project.
        
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        
        return new Notification
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            Message = message,
            Level = level,
            Source = source,
            Timestamp = timeProvider.GetUtcNow(),
            WasForwarded = false
        };
    }
    
    public bool ShouldForward() => Level >= NotificationLevel.Warning;

    public void MarkAsForwarded() => WasForwarded = true;
    
    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;
    
    public string Message { get; private set; } = string.Empty;
    
    public string? Source { get; private set; }
    
    public NotificationLevel Level { get; private set; }
    
    public DateTimeOffset Timestamp { get; private set; }
    
    public bool WasForwarded { get; private set; }
}