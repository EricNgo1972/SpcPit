namespace SPC.Infrastructure.TvanSubmission.Viettel;

/// <summary>
/// Caches the Viettel T-VAN access token across submissions. Thread-safe; refreshes when
/// the cached token is about to expire (configurable skew).
/// </summary>
internal sealed class ViettelTokenStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTime _expiresAtUtc;

    public async Task<string> GetAsync(Func<CancellationToken, Task<(string Token, DateTime ExpiresAtUtc)>> factory,
        int refreshSkewSeconds, CancellationToken ct)
    {
        var thresholdUtc = DateTime.UtcNow.AddSeconds(refreshSkewSeconds);
        if (_token is not null && _expiresAtUtc > thresholdUtc)
            return _token;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_token is not null && _expiresAtUtc > thresholdUtc)
                return _token;

            var (token, expiresAtUtc) = await factory(ct).ConfigureAwait(false);
            _token = token;
            _expiresAtUtc = expiresAtUtc;
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate()
    {
        _token = null;
        _expiresAtUtc = DateTime.MinValue;
    }
}
