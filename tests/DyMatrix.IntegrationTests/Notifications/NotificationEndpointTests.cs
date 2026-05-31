using System.Net;
using System.Net.Http.Json;
using DyMatrix.Application.Notifications;
using DyMatrix.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DyMatrix.IntegrationTests.Notifications;

public sealed class NotificationEndpointTests : IClassFixture<NotificationApiFactory>, IAsyncLifetime
{
    private readonly NotificationApiFactory _factory;
    private readonly HttpClient _client;

    public NotificationEndpointTests(NotificationApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public ValueTask InitializeAsync() => new (Task.CompletedTask);

    public ValueTask DisposeAsync()
    {
        // Reset substitutes between tests to avoid state bleed
        _factory.ChatClient.ClearReceivedCalls();
        _factory.Forwarder.ClearReceivedCalls();
        return new ValueTask(Task.CompletedTask);
    }

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostNotification_WithInfoLevel_Returns202NotForwarded()
    {
        // Arrange
        var request = new
        {
            title = "Scheduled job completed",
            message = "Nightly backup finished successfully.",
            level = "information"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        body!.Forwarded.Should().BeFalse();

        await _factory.ChatClient.DidNotReceive().GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());

        await _factory.Forwarder.DidNotReceive()
            .ForwardAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("critical")]
    public async Task PostNotification_WithForwardableLevel_Returns202Forwarded(string level)
    {
        // Arrange
        var discordMessage = $"⚠️ Alert from TestService [{level}]";
        var chatResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, discordMessage)]);

        _factory.ChatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        var request = new
        {
            title = "Something went wrong",
            message = "Details about the issue.",
            level,
            source = "TestService"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        body!.Forwarded.Should().BeTrue();

        await _factory.Forwarder.Received(1)
            .ForwardAsync(discordMessage, Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Validation failures → 400
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostNotification_WithEmptyTitle_Returns400()
    {
        var request = new { title = "", message = "Message.", level = "info" };

        var response = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem!.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task PostNotification_WithInvalidLevel_Returns400WithLevelError()
    {
        var request = new { title = "Title", message = "Message.", level = "debug" };

        var response = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem!.Errors.Should().ContainKey("Level");
    }

    [Fact]
    public async Task PostNotification_WithFutureTimestamp_Returns400()
    {
        var request = new
        {
            title = "Title",
            message = "Message.",
            level = "info",
            timestamp = DateTimeOffset.UtcNow.AddHours(1)
        };

        var response = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Resilience — LLM failures
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostNotification_WhenLlmFails_StillForwardsWithFallback()
    {
        // Arrange
        _factory.ChatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("LLM unreachable"));

        var request = new
        {
            title = "DB timeout",
            message = "Connection lost.",
            level = "error",
            source = "OrderService"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        body!.Forwarded.Should().BeTrue();

        await _factory.Forwarder.Received(1).ForwardAsync(
            Arg.Is<string>(msg => msg.Contains("[ERROR]") && msg.Contains("DB timeout")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostNotification_WhenDiscordFails_Returns202NotForwarded()
    {
        // Arrange
        var chatResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Alert")]);
        _factory.ChatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        _factory.Forwarder
            .ForwardAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Discord unreachable"));

        var request = new { title = "Title", message = "Message.", level = "error" };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        body!.Forwarded.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Rate limiting → 429
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostNotification_WhenRateLimitExceeded_Returns429WithRetryAfterHeader()
    {
        // Arrange
        var chatResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Alert")]);
        _factory.ChatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        var request = new { title = "Title", message = "Message.", level = "warning" };

        // Act — send 11 requests; first 10 should succeed, 11th should be rate limited
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 11; i++)
            lastResponse = await _client.PostAsJsonAsync("/notifications", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        lastResponse.Headers.Should().ContainKey("Retry-After");
    }
}