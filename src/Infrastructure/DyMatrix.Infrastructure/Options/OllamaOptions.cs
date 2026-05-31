namespace DyMatrix.Infrastructure.Options;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; init; } = "http://localhost:11434";
}

[OptionsValidator]
public partial class OllamaOptionsValidator : IValidateOptions<OllamaOptions>;