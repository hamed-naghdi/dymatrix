using DyMatrix.Application.Notifications;
using FluentValidation.TestHelper;

namespace DyMatrix.UnitTests.Application;

public sealed class NotificationRequestValidatorTests
{
    private readonly NotificationRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldHaveNoErrors()
    {
        // Arrange
        var request = new NotificationRequest("Title", "Message", "warning", "TestService", DateTimeOffset.UtcNow.AddMinutes(-1));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public void Validate_WithMinimalValidRequest_ShouldHaveNoErrors()
    {
        // Arrange — source and timestamp are optional
        var request = new NotificationRequest("Title", "Message", "information", null, null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyTitle_ShouldHaveValidationError(string title)
    {
        var request = new NotificationRequest(title, "Message", "information", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithTitleExceedingMaxLength_ShouldHaveValidationError()
    {
        var request = new NotificationRequest(new string('a', 201), "Message", "information", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyMessage_ShouldHaveValidationError(string message)
    {
        var request = new NotificationRequest("Title", message, "information", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void Validate_WithMessageExceedingMaxLength_ShouldHaveValidationError()
    {
        var request = new NotificationRequest("Title", new string('a', 2001), "information", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Theory]
    [InlineData("debug")]
    [InlineData("verbose")]
    [InlineData("trace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidLevel_ShouldHaveValidationError(string level)
    {
        var request = new NotificationRequest("Title", "Message", level, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Level);
    }

    [Theory]
    [InlineData("information")]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("critical")]
    [InlineData("INFORMATION")]
    [InlineData("WARNING")]
    [InlineData("Error")]
    public void Validate_WithValidLevel_ShouldNotHaveValidationError(string level)
    {
        var request = new NotificationRequest("Title", "Message", level, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Level);
    }

    [Fact]
    public void Validate_WithSourceExceedingMaxLength_ShouldHaveValidationError()
    {
        var request = new NotificationRequest("Title", "Message", "information", new string('a', 101), null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Source);
    }

    [Fact]
    public void Validate_WithFutureTimestamp_ShouldHaveValidationError()
    {
        var request = new NotificationRequest("Title", "Message", "information", null, DateTimeOffset.UtcNow.AddHours(1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    [Fact]
    public void Validate_WithPastTimestamp_ShouldNotHaveValidationError()
    {
        var request = new NotificationRequest("Title", "Message", "information", null, DateTimeOffset.UtcNow.AddHours(-1));
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Timestamp);
    }

    [Fact]
    public void Validate_WithNullTimestamp_ShouldNotHaveValidationError()
    {
        var request = new NotificationRequest("Title", "Message", "information", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Timestamp);
    }
}