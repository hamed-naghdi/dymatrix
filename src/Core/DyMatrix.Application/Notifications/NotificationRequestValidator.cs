using DyMatrix.Domain.Enums;

namespace DyMatrix.Application.Notifications;

public sealed class NotificationRequestValidator : AbstractValidator<NotificationRequest>
{
    private static readonly string[] ValidLevels =
        Enum.GetNames<NotificationLevel>().Select(n => n.ToLowerInvariant()).ToArray();
    
    public NotificationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000).WithMessage("Message must not exceed 2000 characters.");

        RuleFor(x => x.Level)
            .NotEmpty().WithMessage("Level is required.")
            .Must(l => ValidLevels.Contains(l.ToLowerInvariant()))
            .WithMessage($"Level must be one of: {string.Join(", ", ValidLevels)}.");

        RuleFor(x => x.Source)
            .MaximumLength(100).WithMessage("Source must not exceed 100 characters.")
            .When(x => x.Source is not null);

        RuleFor(x => x.Timestamp)
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow)
            .WithMessage("Timestamp cannot be in the future.")
            .When(x => x.Timestamp.HasValue);
    }
}