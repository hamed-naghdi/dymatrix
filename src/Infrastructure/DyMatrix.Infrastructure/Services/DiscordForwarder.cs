using System.Net.Http.Json;

namespace DyMatrix.Infrastructure.Services;

public class DiscordForwarder : INotificationForwarder
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscordForwarder> _logger;

    public DiscordForwarder(
        HttpClient httpClient,
        ILogger<DiscordForwarder> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task ForwardAsync(string message, CancellationToken cancellationToken = default)
    {
        var payload = new { content = message };

        var response = await _httpClient.PostAsJsonAsync(
            string.Empty,
            payload,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Discord webhook returned {StatusCode}. Body: {Body}",
                (int)response.StatusCode, body);

            throw new HttpRequestException(
                $"Discord webhook failed with status {(int)response.StatusCode}.");
        }

        _logger.LogInformation("Message successfully sent to Discord.");
    }
}