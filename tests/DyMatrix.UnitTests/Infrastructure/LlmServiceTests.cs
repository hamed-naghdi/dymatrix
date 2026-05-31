using DyMatrix.Domain.Enums;
using DyMatrix.Infrastructure.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace DyMatrix.UnitTests.Infrastructure;

public sealed class LlmServiceTests : IDisposable
{
    private readonly IChatClient _chatClient = Substitute.For<IChatClient>();
    private readonly RateLimiterService _rateLimiter = new();
    private readonly ILogger<OpenAiLlmService> _logger = Substitute.For<ILogger<OpenAiLlmService>>();

    private OpenAiLlmService CreateSut(int timeoutSeconds = 30) =>
        new(_chatClient,
            _rateLimiter,
            Options.Create(new LlmOptions { TimeoutSeconds = timeoutSeconds }),
            _logger);

    private static Notification BuildNotification(NotificationLevel level = NotificationLevel.Warning) =>
        Notification.Create("Test title", "Test message", level, TimeProvider.System, "TestService");

    [Fact]
    public async Task GenerateMessageAsync_WhenChatClientSucceeds_ShouldReturnMessage()
    {
        // Arrange
        var expected = "⚠️ High memory usage on worker-3.";
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, expected)]);
        _chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var sut = CreateSut();

        // Act
        var result = await sut.GenerateMessageAsync(BuildNotification(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GenerateMessageAsync_WhenChatClientReturnsEmptyText_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, string.Empty)]);
        _chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(response);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.GenerateMessageAsync(BuildNotification());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task GenerateMessageAsync_WhenRateLimitExceeded_ShouldThrowRateLimitExceededException()
    {
        // Arrange — exhaust the rate limiter
        for (var i = 0; i < 10; i++)
            await _rateLimiter.AcquireAsync(TestContext.Current.CancellationToken);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.GenerateMessageAsync(BuildNotification());

        // Assert
        await act.Should().ThrowAsync<RateLimitExceededException>();
        await _chatClient.DidNotReceive().GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMessageAsync_WhenChatClientThrows_ShouldPropagateException()
    {
        // Arrange
        _chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("OpenAI unreachable"));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.GenerateMessageAsync(BuildNotification());

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public void Dispose() => _rateLimiter.Dispose();
}