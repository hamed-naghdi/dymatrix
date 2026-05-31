using DyMatrix.Application.Common.Interfaces;
using DyMatrix.Application.Notifications;
using NSubstitute.ExceptionExtensions;

namespace DyMatrix.UnitTests.Application;

public sealed class NotificationHandlerTests
{
    private readonly ILlmService _llmService = Substitute.For<ILlmService>();
    private readonly INotificationForwarder _forwarder = Substitute.For<INotificationForwarder>();
    private readonly ILogger<NotificationHandler> _logger = Substitute.For<ILogger<NotificationHandler>>();
    
    private NotificationHandler CreateNotificationHandlerSut() => new(_llmService, _forwarder, TimeProvider.System, _logger);
    
    [Fact]
    public async Task Handle_WithInfoLevel_ShouldNotForward()
    {
        // Arrange
        var request = new NotificationRequest("Title", "Message", "information", null, null);
        
        var notificationHandler = CreateNotificationHandlerSut();
        
        // Act
        var result = await notificationHandler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Forwarded.Should().BeFalse();
        await _llmService.DidNotReceive().GenerateMessageAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _forwarder.DidNotReceive().ForwardAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("critical")]
    public async Task Handle_WithForwardableLevel_ShouldCallLlmAndForwarder(string level)
    {
        // Arrange
        var request = new NotificationRequest("Title", "Message", level, "TestService", null);
        _llmService.GenerateMessageAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns("Generated Discord message");

        var notificationHandler = CreateNotificationHandlerSut();

        // Act
        var result = await notificationHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Forwarded.Should().BeTrue();
        await _llmService.Received(1).GenerateMessageAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _forwarder.Received(1).ForwardAsync("Generated Discord message", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Handle_WhenLlmFails_ShouldUseFallbackAndStillForward()
    {
        // Arrange
        var request = new NotificationRequest("Title", "Message", "error", "OrderService", null);
        _llmService.GenerateMessageAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("LLM unreachable"));

        var notificationHandler = CreateNotificationHandlerSut();

        // Act
        var result = await notificationHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Forwarded.Should().BeTrue();
        await _forwarder.Received(1).ForwardAsync(
            Arg.Is<string>(msg => msg.Contains("[ERROR]") && msg.Contains("Title")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLlmThrowsRateLimitException_ShouldPropagate()
    {
        // Arrange
        var request = new NotificationRequest("Title", "Message", "error", null, null);
        _llmService.GenerateMessageAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RateLimitExceededException(10));
        
        var notificationHandler = CreateNotificationHandlerSut();
        
        // Act
        var act = async () => await notificationHandler.Handle(request, CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<RateLimitExceededException>();
        await _forwarder.DidNotReceive().ForwardAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Handle_WhenDiscordFails_ShouldReturnNotForwarded()
    {
        // Arrange
        var request = new NotificationRequest("Title", "Message", "error", null, null);
        _llmService.GenerateMessageAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns("Discord message");
        _forwarder.ForwardAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Discord unreachable"));

        var notificationHandler = CreateNotificationHandlerSut();

        // Act
        var result = await notificationHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Forwarded.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenLlmFails_FallbackMessage_ShouldContainExpectedFields()
    {
        // Arrange
        var request = new NotificationRequest("DB Down", "Connection lost", "critical", "PaymentService", null);
        _llmService.GenerateMessageAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("LLM down"));

        string? capturedMessage = null;
        await _forwarder.ForwardAsync(
            Arg.Do<string>(msg => capturedMessage = msg),
            Arg.Any<CancellationToken>());

        var notificationHandler = CreateNotificationHandlerSut();

        // Act
        await notificationHandler.Handle(request, CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage.Should().Contain("[CRITICAL]");
        capturedMessage.Should().Contain("DB Down");
        capturedMessage.Should().Contain("PaymentService");
        capturedMessage.Should().Contain("Connection lost");
    }
}