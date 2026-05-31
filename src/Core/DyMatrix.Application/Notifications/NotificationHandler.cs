using DyMatrix.Application.Common.Interfaces;
using DyMatrix.Domain.Entities;
using DyMatrix.Domain.Enums;
using DyMatrix.Domain.Exceptions;

namespace DyMatrix.Application.Notifications;

public partial class NotificationHandler : IRequestHandler<NotificationRequest, NotificationResponse>
{
    private readonly ILlmService _llmService;
    private readonly INotificationForwarder _forwarder;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NotificationHandler> _logger;

    public NotificationHandler(
        ILlmService llmService,
        INotificationForwarder forwarder,
        TimeProvider timeProvider,
        ILogger<NotificationHandler> logger)
    {
        _llmService = llmService;
        _forwarder = forwarder;
        _timeProvider = timeProvider;
        _logger = logger;
    }
    
    public async Task<NotificationResponse> Handle(NotificationRequest request, CancellationToken cancellationToken)
    {
        // safe because validator already confirmed it's valid
        var level = Enum.Parse<NotificationLevel>(request.Level, ignoreCase: true);
        
        // just to make 100% sure.
        Guard.Against.Null(level);
        
        var notification = Notification.Create(
            request.Title,
            request.Message,
            level,
            _timeProvider,
            request.Source);
        
        // Short-circuit if no forwarding needed
        if (!notification.ShouldForward())
        {
            LogNotificationIdWithLevelLevelReceivedNoForwardingRequired(notification.Id, notification.Level);

            return new NotificationResponse(notification.Id, Forwarded: false);
        }
        
        // RateLimitExceededException is intentionally not caught here —
        // it propagates up to the API layer which maps it to 429.
        string discordMessage;
        try
        {
            discordMessage = await _llmService.GenerateMessageAsync(notification, cancellationToken);
        }
        catch (Exception ex) when (ex is not RateLimitExceededException)
        {
            LogLlmServiceFailedForNotificationIdUsingFallbackMessage(notification.Id, ex);

            discordMessage = BuildFallbackMessage(notification);
        }
        
        // Forward to Discord
        try
        {
            await _forwarder.ForwardAsync(discordMessage, cancellationToken);
            notification.MarkAsForwarded();

            LogNotificationIdWithLevelLevelSuccessfullyForwarded(notification.Id, notification.Level);
        }
        catch (Exception ex)
        {
            LogDiscordForwarderFailedForNotificationIdMessageWasNotDelivered(notification.Id, ex);
        }
        
        return new NotificationResponse(notification.Id, notification.WasForwarded);
    }
    
    private static string BuildFallbackMessage(Notification notification) =>
        $"[{notification.Level.ToString().ToUpperInvariant()}] {notification.Title}\n" +
        $"Source: {notification.Source ?? "unknown"}\n" +
        $"Message: {notification.Message}\n" +
        $"Time: {notification.Timestamp:u}";

    [LoggerMessage(LogLevel.Information, "Notification {Id} with level {Level} received. No forwarding required.")]
    partial void LogNotificationIdWithLevelLevelReceivedNoForwardingRequired(Guid id, NotificationLevel level);

    [LoggerMessage(LogLevel.Error, "LLM service failed for notification {Id}. Using fallback message.")]
    partial void LogLlmServiceFailedForNotificationIdUsingFallbackMessage(Guid id, Exception exception);

    [LoggerMessage(LogLevel.Information, "Notification {Id} with level {Level} successfully forwarded.")]
    partial void LogNotificationIdWithLevelLevelSuccessfullyForwarded(Guid id, NotificationLevel level);

    [LoggerMessage(LogLevel.Error, "Discord forwarder failed for notification {Id}. Message was not delivered.")]
    partial void LogDiscordForwarderFailedForNotificationIdMessageWasNotDelivered(Guid id, Exception exception);
}