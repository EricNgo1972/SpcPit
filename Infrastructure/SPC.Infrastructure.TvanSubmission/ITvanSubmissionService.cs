namespace SPC.Infrastructure.TvanSubmission;

/// <summary>Response from a T-VAN provider after submitting a signed PIT cert XML.</summary>
public sealed record TvanSubmissionResponse(bool Accepted, string? CqtCode, string? RejectReason, string Raw);

/// <summary>
/// Abstraction for pushing signed XML to a T-VAN provider (Viettel, VNPT, MISA, etc.),
/// which forwards to the General Department of Taxation and returns the mã CQT.
/// </summary>
public interface ITvanSubmissionService
{
    Task<TvanSubmissionResponse> SubmitAsync(byte[] signedXml, CancellationToken cancellationToken = default);
}
