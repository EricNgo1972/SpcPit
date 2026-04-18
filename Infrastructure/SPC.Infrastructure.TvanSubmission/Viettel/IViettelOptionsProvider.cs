namespace SPC.Infrastructure.TvanSubmission.Viettel;

/// <summary>
/// Resolves the current Viettel connection settings at submission time. The default
/// implementation loads them from the KeyValueStore-backed <c>ViettelSettings</c> BO,
/// so UI edits take effect immediately without an app restart. Tests replace this with
/// a static provider.
/// </summary>
public interface IViettelOptionsProvider
{
    Task<ViettelOptions> GetAsync(CancellationToken cancellationToken = default);
}
