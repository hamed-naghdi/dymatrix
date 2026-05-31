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
        string? source = null,
        DateTimeOffset? timestamp = null)
    {
        // Avoided using value objects with DDD pattern for such a small test project.
        
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        
        return new Notification
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            Message = message,
            Level = level,
            Source = source,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
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