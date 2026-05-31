using DyMatrix.Domain.Entities;
using DyMatrix.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DyMatrix.UnitTests.Domain;

public sealed class NotificationTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldReturnNotification()
    {
        // Arrange & Act
        var notification = Notification.Create("Title", "Message", NotificationLevel.Information);
        
        // Assert
        notification.Should().NotBeNull();
        notification.Id.Should().NotBe(Guid.Empty);
        notification.Title.Should().Be("Title");
        notification.Message.Should().Be("Message");
        notification.Level.Should().Be(NotificationLevel.Information);
        notification.Source.Should().BeNull();
        notification.WasForwarded.Should().BeFalse();
        notification.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithAllFields_ShouldMapCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Act
        var notification = Notification.Create("Title", "Message", NotificationLevel.Error, "OrderService", timestamp);

        // Assert
        notification.Source.Should().Be("OrderService");
        notification.Timestamp.Should().Be(timestamp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ShouldThrowArgumentException(string title)
    {
        // Act
        var act = () => Notification.Create(title, "Message", NotificationLevel.Information);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(title));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyMessage_ShouldThrowArgumentException(string message)
    {
        // Act
        var act = () => Notification.Create("Title", message, NotificationLevel.Information);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(message));
    }

    [Theory]
    [InlineData(NotificationLevel.Information, false)]
    [InlineData(NotificationLevel.Warning, true)]
    [InlineData(NotificationLevel.Error, true)]
    [InlineData(NotificationLevel.Critical, true)]
    public void ShouldForward_ShouldReturnCorrectResult(NotificationLevel level, bool expected)
    {
        // Arrange
        var notification = Notification.Create("Title", "Message", level);
        
        // Act
        var result = notification.ShouldForward();
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Fact]
    public void MarkAsForwarded_ShouldSetWasForwardedToTrue()
    {
        // Arrange
        var notification = Notification.Create("Title", "Message", NotificationLevel.Warning);

        // Act
        notification.MarkAsForwarded();

        // Assert
        notification.WasForwarded.Should().BeTrue();
    }
}