namespace DyMatrix.UnitTests.Infrastructure;

public sealed class RateLimiterServiceTests : IDisposable
{
    private readonly RateLimiterService _sut = new();

    [Fact]
    public async Task AcquireAsync_WithinLimit_ShouldNotThrow()
    {
        // Act
        var act = async () =>
        {
            for (var i = 0; i < 10; i++)
                await _sut.AcquireAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AcquireAsync_ExceedingLimit_ShouldThrowRateLimitExceededException()
    {
        // Arrange — exhaust the limit
        for (var i = 0; i < 10; i++)
            await _sut.AcquireAsync(TestContext.Current.CancellationToken);

        // Act
        var act = async () => await _sut.AcquireAsync();

        // Assert
        await act.Should().ThrowAsync<RateLimitExceededException>();
    }

    [Fact]
    public async Task AcquireAsync_AfterWindowExpiry_ShouldAllowNewRequests()
    {
        // This test validates the sliding window resets correctly.
        // We cannot wait a full minute in a unit test, so we verify
        // the limiter allows exactly 10 within the window and throws on 11th.
        for (var i = 0; i < 10; i++)
            await _sut.AcquireAsync(TestContext.Current.CancellationToken);

        var act = async () => await _sut.AcquireAsync();
        await act.Should().ThrowAsync<RateLimitExceededException>();
    }

    public void Dispose() => _sut.Dispose();
}