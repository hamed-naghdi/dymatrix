namespace DyMatrix.Infrastructure.Options;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    [Required(AllowEmptyStrings = false)]
    public string Provider { get; init; } = "openai"; // "openai" | "ollama"

    [Required(AllowEmptyStrings = false)]
    public string ModelId { get; init; } = "gpt-4o";

    // Only required when Provider = "openai"
    public string? ApiKey { get; init; }

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 15;
}

[OptionsValidator]
public partial class LlmOptionsValidator : IValidateOptions<LlmOptions>;