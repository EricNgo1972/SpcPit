using Microsoft.Extensions.Logging;

namespace SPC.Infrastructure.XmlSigning;

/// <summary>MVP default: returns the input unchanged so the lifecycle can be exercised end-to-end without a real signing agent.</summary>
public sealed class StubXmlSigningService : IXmlSigningService
{
    private readonly ILogger<StubXmlSigningService> _logger;

    public StubXmlSigningService(ILogger<StubXmlSigningService> logger) => _logger = logger;

    public Task<byte[]> SignAsync(byte[] unsignedXml, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unsignedXml);
        _logger.LogInformation("StubXmlSigningService: pretending to sign {Bytes} bytes", unsignedXml.Length);
        return Task.FromResult(unsignedXml);
    }
}
