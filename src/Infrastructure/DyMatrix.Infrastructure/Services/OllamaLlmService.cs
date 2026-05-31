using DyMatrix.Domain.Entities;
using DyMatrix.Domain.Enums;

namespace DyMatrix.Infrastructure.Services;

public sealed partial class OllamaLlmService : ILlmService
{
    private readonly IChatClient _chatClient;
    private readonly RateLimiterService _rateLimiter;
    private readonly ILogger<OllamaLlmService> _logger;
    private readonly int _timeoutSeconds;
    private readonly string _modelId;

    public OllamaLlmService(
        IChatClient chatClient,
        RateLimiterService rateLimiter,
        IOptions<LlmOptions> options,
        ILogger<OllamaLlmService> logger)
    {
        _chatClient = chatClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _modelId = options.Value.ModelId;
        _timeoutSeconds = options.Value.TimeoutSeconds;
    }

    public async Task<string> GenerateMessageAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await _rateLimiter.AcquireAsync(cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

        LogCallingOllamaModelForNotificationIdWithLevelLevel(_modelId, notification.Id, notification.Level);

        var response = await _chatClient.GetResponseAsync(
            BuildPrompt(notification),
            cancellationToken: cts.Token);

        return string.IsNullOrWhiteSpace(response.Text) 
            ? throw new InvalidOperationException("Ollama returned an empty response.")
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

         /no_think
         """;

    [LoggerMessage(LogLevel.Information, "Calling Ollama ({Model}) for notification {Id} with level {Level}.")]
    partial void LogCallingOllamaModelForNotificationIdWithLevelLevel(string model, Guid id, NotificationLevel level);
}