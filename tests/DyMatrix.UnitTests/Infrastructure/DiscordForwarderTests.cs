using System.Net;
using System.Text.Json;
using DyMatrix.UnitTests.Helpers;

namespace DyMatrix.UnitTests.Infrastructure;

public sealed class DiscordForwarderTests
{
    private static DiscordForwarder CreateSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://discord.com/api/webhooks/test/token"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        var logger = Substitute.For<ILogger<DiscordForwarder>>();

        return new DiscordForwarder(httpClient, logger);
    }

    [Fact]
    public async Task ForwardAsync_WhenDiscordReturns204_ShouldNotThrow()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
        var sut = CreateSut(handler);

        // Act
        var act = async () => await sut.ForwardAsync("Test message");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForwardAsync_WhenDiscordReturns400_ShouldThrowHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "Bad Request");
        var sut = CreateSut(handler);

        // Act
        var act = async () => await sut.ForwardAsync("Test message");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ForwardAsync_ShouldSendCorrectPayload()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent, captureBody: body => capturedBody = body);
        var sut = CreateSut(handler);

        // Act
        await sut.ForwardAsync("Alert: something went wrong", TestContext.Current.CancellationToken);

        // Assert
        capturedBody.Should().NotBeNull();
        var payload = JsonSerializer.Deserialize<JsonElement>(capturedBody!);
        payload.GetProperty("content").GetString().Should().Be("Alert: something went wrong");
    }
}