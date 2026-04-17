using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SPC.Infrastructure.XmlSigning;

/// <summary>
/// Calls an existing LocalAgent signing service over HTTP. POSTs the unsigned XML bytes to
/// the configured endpoint with <c>Content-Type: application/xml</c> and expects signed XML
/// bytes back in the response body. The exact contract (headers, error envelope, certificate
/// selection) will likely need tuning once the real LocalAgent spec is confirmed.
/// </summary>
public sealed class LocalAgentXmlSigningService : IXmlSigningService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<XmlSigningOptions> _options;
    private readonly ILogger<LocalAgentXmlSigningService> _logger;

    public LocalAgentXmlSigningService(
        HttpClient httpClient,
        IOptionsMonitor<XmlSigningOptions> options,
        ILogger<LocalAgentXmlSigningService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<byte[]> SignAsync(byte[] unsignedXml, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unsignedXml);
        var endpoint = _options.CurrentValue.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("XmlSigning:Endpoint is not configured.");

        using var content = new ByteArrayContent(unsignedXml);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

        _logger.LogInformation("LocalAgent sign: POST {Endpoint} ({Bytes} bytes)", endpoint, unsignedXml.Length);
        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
