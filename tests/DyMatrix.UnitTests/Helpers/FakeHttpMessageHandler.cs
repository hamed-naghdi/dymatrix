using System.Net;

namespace DyMatrix.UnitTests.Helpers;

/// <summary>
/// Minimal fake HTTP handler — avoids Moq/NSubstitute for HttpMessageHandler.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;
    private readonly Action<string>? _captureBody;

    public FakeHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseBody = "",
        Action<string>? captureBody = null)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
        _captureBody = captureBody;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_captureBody is not null && request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            _captureBody(body);
        }

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody)
        };
    }
}