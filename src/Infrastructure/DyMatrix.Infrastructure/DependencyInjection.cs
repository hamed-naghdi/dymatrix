using DyMatrix.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace DyMatrix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Options
        services
            .AddOptions<LlmOptions>()
            .BindConfiguration(LlmOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<LlmOptions>, LlmOptionsValidator>();

        services
            .AddOptions<OllamaOptions>()
            .BindConfiguration(OllamaOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OllamaOptions>, OllamaOptionsValidator>();

        services
            .AddOptions<DiscordOptions>()
            .BindConfiguration(DiscordOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DiscordOptions>, DiscordOptionsValidator>();

        // Rate limiter — singleton: shared state across all requests
        services.AddSingleton<RateLimiterService>();

        // IChatClient — singleton: stateless, thread-safe
        // LLM — provider selected based on configuration
        services.AddSingleton<IChatClient>(sp =>
        {
            var llmOptions = sp.GetRequiredService<IOptions<LlmOptions>>().Value;

            return llmOptions.Provider.ToLowerInvariant() switch
            {
                "ollama" => new OllamaApiClient(
                    new Uri(sp.GetRequiredService<IOptions<OllamaOptions>>().Value.Endpoint),
                    llmOptions.ModelId),

                _ => new OpenAI.Chat.ChatClient(llmOptions.ModelId, llmOptions.ApiKey)
                    .AsIChatClient()
            };
        });

        services.AddSingleton<ILlmService>(sp =>
        {
            var llmOptions = sp.GetRequiredService<IOptions<LlmOptions>>().Value;

            return llmOptions.Provider.ToLowerInvariant() switch
            {
                "ollama" => ActivatorUtilities.CreateInstance<OllamaLlmService>(sp),
                _ => ActivatorUtilities.CreateInstance<OpenAiLlmService>(sp)
            };
        });

        services.AddScoped<ILlmService, OpenAiLlmService>();

        // Discord — typed HttpClient managed by IHttpClientFactory
        services.AddHttpClient<INotificationForwarder, DiscordForwarder>((sp, client) =>
        {
            var discordOptions = sp.GetRequiredService<IOptions<DiscordOptions>>().Value;
            client.BaseAddress = new Uri(discordOptions.WebhookUrl);
            client.Timeout = TimeSpan.FromSeconds(discordOptions.TimeoutSeconds);
        });
        
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}