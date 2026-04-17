using Microsoft.Extensions.Logging;

namespace SPC.Infrastructure.TvanSubmission;

/// <summary>MVP default: returns an accepted response with a synthetic CQT code.</summary>
public sealed class StubTvanSubmissionService : ITvanSubmissionService
{
    private readonly ILogger<StubTvanSubmissionService> _logger;

    public StubTvanSubmissionService(ILogger<StubTvanSubmissionService> logger) => _logger = logger;

    public Task<TvanSubmissionResponse> SubmitAsync(byte[] signedXml, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signedXml);
        var cqtCode = "STUB-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        _logger.LogInformation("StubTvanSubmissionService: {Bytes} bytes → {CqtCode}", signedXml.Length, cqtCode);
        return Task.FromResult(new TvanSubmissionResponse(
            Accepted: true,
            CqtCode: cqtCode,
            RejectReason: null,
            Raw: "{\"status\":\"stub-accepted\"}"));
    }
}
