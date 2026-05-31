namespace DyMatrix.Application.Notifications;

public sealed record NotificationRequest(
    string Title,
    string Message,
    string Level,
    string? Source,
    DateTimeOffset? Timestamp
) : IRequest<NotificationResponse>;