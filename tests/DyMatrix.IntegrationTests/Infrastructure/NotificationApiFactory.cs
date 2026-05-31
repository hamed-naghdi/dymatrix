using DyMatrix.Application.Common.Interfaces;
using DyMatrix.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace DyMatrix.IntegrationTests.Infrastructure;

public sealed class NotificationApiFactory : WebApplicationFactory<Program>
{
    public IChatClient ChatClient { get; } = Substitute.For<IChatClient>();
    public INotificationForwarder Forwarder { get; } = Substitute.For<INotificationForwarder>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Inject dummy config values so ValidateOnStart passes
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Llm:Provider"]        = "openai",
                ["Llm:ModelId"]         = "gpt-4o",
                ["Llm:ApiKey"]          = "test-api-key",
                ["Llm:TimeoutSeconds"]  = "50",
                ["Ollama:Endpoint"]     = "http://localhost:11434",
                ["Discord:WebhookUrl"]  = "https://discord.com/api/webhooks/test/token",
                ["Discord:TimeoutSeconds"] = "30"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace IChatClient with fake
            services.RemoveAll<IChatClient>();
            services.AddSingleton(ChatClient);

            // Replace INotificationForwarder with fake
            services.RemoveAll<INotificationForwarder>();
            services.AddScoped(_ => Forwarder);

            // Fresh RateLimiterService per factory — no state bleed between tests
            services.RemoveAll<RateLimiterService>();
            services.AddSingleton<RateLimiterService>();
        });
    }
}