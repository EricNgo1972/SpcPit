using Microsoft.Extensions.Logging;

namespace SPC.Infrastructure.TvanSubmission;

/// <summary>MVP default: returns an accepted response with synthetic identifiers.</summary>
public sealed class StubTvanSubmissionService : ITvanSubmissionService
{
    private readonly ILogger<StubTvanSubmissionService> _logger;

    public StubTvanSubmissionService(ILogger<StubTvanSubmissionService> logger) => _logger = logger;

    public Task<TvanSubmissionResponse> SubmitAsync(TvanSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var response = new TvanSubmissionResponse(
            Accepted: true,
            CqtCode: "STUB-" + suffix,
            RejectReason: null,
            Raw: "{\"status\":\"stub-accepted\"}",
            InvoiceNo: $"CT/25E{suffix[..6]}",
            TransactionId: $"STUB-TX-{suffix}",
            ReservationCode: "STUB-RES-" + suffix);
        _logger.LogInformation("StubTvanSubmissionService: {Cert} → {CqtCode}",
            request.Certificate.ProformaNo, response.CqtCode);
        return Task.FromResult(response);
    }
}
