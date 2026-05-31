namespace DyMatrix.Infrastructure.Options;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    [Required(AllowEmptyStrings = false)]
    public string WebhookUrl { get; init; } = string.Empty;

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;
}

[OptionsValidator]
public partial class DiscordOptionsValidator : IValidateOptions<DiscordOptions>;