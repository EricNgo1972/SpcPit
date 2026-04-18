using SPC.BO.PIT.Xml;

namespace SPC.Infrastructure.TvanSubmission;

/// <summary>Data the T-VAN provider needs to submit a PIT withholding certificate.</summary>
/// <remarks>
/// Different providers want different shapes: Viettel wants structured JSON (and signs internally),
/// a generic "signed-XML" provider wants the bytes. Include both so the adapter can pick.
/// </remarks>
public sealed record TvanSubmissionRequest(
    PitCertificateXmlInput Certificate,
    PitSettingsXmlInput Settings,
    byte[]? SignedXml = null);

/// <summary>
/// Response from a T-VAN provider after submitting a PIT cert.
/// <see cref="CqtCode"/> is the mã xác thực from the tax authority (usually Viettel's
/// <c>codeOfTax</c> or <c>reservationCode</c>). <see cref="InvoiceNo"/> is the provider's
/// certificate number (e.g. <c>CT/25E113</c>). <see cref="TransactionId"/> is the provider's
/// internal trace id for support.
/// </summary>
public sealed record TvanSubmissionResponse(
    bool Accepted,
    string? CqtCode,
    string? RejectReason,
    string Raw,
    string? InvoiceNo = null,
    string? TransactionId = null,
    string? ReservationCode = null);

/// <summary>
/// Abstraction for pushing a PIT certificate to a T-VAN provider (Viettel, VNPT, MISA, etc.).
/// The provider signs if needed, forwards to the General Department of Taxation, and returns
/// identifiers so the cert can be tracked.
/// </summary>
public interface ITvanSubmissionService
{
    Task<TvanSubmissionResponse> SubmitAsync(TvanSubmissionRequest request, CancellationToken cancellationToken = default);
}
