namespace DyMatrix.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    bool Forwarded
);