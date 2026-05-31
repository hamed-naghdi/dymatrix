using DyMatrix.Domain.Entities;
using DyMatrix.Domain.Enums;

namespace DyMatrix.Infrastructure.Services;

public partial class OpenAiLlmService : ILlmService
{
    private readonly IChatClient _chatClient;
    private readonly RateLimiterService _rateLimiter;
    private readonly ILogger<OpenAiLlmService> _logger;
    private readonly int _timeoutSeconds;
    
    public OpenAiLlmService(
        IChatClient chatClient,
        RateLimiterService rateLimiter,
        IOptions<LlmOptions> options,
        ILogger<OpenAiLlmService> logger)
    {
        _chatClient = chatClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _timeoutSeconds = options.Value.TimeoutSeconds;
    }
    
    public async Task<string> GenerateMessageAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // Throws RateLimitExceededException if limit is hit —
        // propagates up through Application to the API middleware → 429
        await _rateLimiter.AcquireAsync(cancellationToken);
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));
        
        LogCallingOpenaiForNotificationIdWithLevelLevel(notification.Id, notification.Level);
        
        var response = await _chatClient.GetResponseAsync(
            BuildPrompt(notification),
            cancellationToken: cts.Token);

        return string.IsNullOrWhiteSpace(response.Text) 
            ? throw new InvalidOperationException("OpenAI returned an empty response.")
            : response.Text;
    }

    private static string BuildPrompt(Notification notification) =>
        $"""
         You are an operations monitoring assistant.
         Analyze the following notification and generate a concise, clear alert message
         suitable for posting in a Discord operations channel.

         The message should:
         - Start with an appropriate severity emoji (⚠️ for warning, 🔴 for error, 🚨 for critical)
         - Include a one-line summary of what went wrong
         - Briefly explain the potential impact
         - Suggest an immediate action if applicable
         - Be at most 5 lines long

         Notification details:
         - Level: {notification.Level}
         - Title: {notification.Title}
         - Message: {notification.Message}
         - Source: {notification.Source ?? "unknown"}
         - Time: {notification.Timestamp:u}
         """;

    [LoggerMessage(LogLevel.Information, "Calling OpenAI for notification {Id} with level {Level}.")]
    partial void LogCallingOpenaiForNotificationIdWithLevelLevel(Guid id, NotificationLevel level);
}