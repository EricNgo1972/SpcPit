using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SPC.Infrastructure.TvanSubmission.Viettel;

/// <summary>
/// Submits PIT certificates to Viettel's T-VAN gateway per the v1.0 integration guide
/// (<c>createTaxDeductionCertificate</c>, CTS Server flow). Configuration is loaded from
/// <see cref="IViettelOptionsProvider"/> per call so Blazor UI edits (KeyValueStore-backed
/// <c>ViettelSettings</c>) take effect immediately.
/// </summary>
public sealed class ViettelTvanSubmissionService : ITvanSubmissionService
{
    private readonly HttpClient _httpClient;
    private readonly IViettelOptionsProvider _optionsProvider;
    private readonly ILogger<ViettelTvanSubmissionService> _logger;
    private readonly ViettelTokenStore _tokenStore = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ViettelTvanSubmissionService(
        HttpClient httpClient,
        IViettelOptionsProvider optionsProvider,
        ILogger<ViettelTvanSubmissionService> logger)
    {
        _httpClient = httpClient;
        _optionsProvider = optionsProvider;
        _logger = logger;
    }

    public async Task<TvanSubmissionResponse> SubmitAsync(TvanSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cfg = await _optionsProvider.GetAsync(cancellationToken);
        RequireConfigured(cfg);

        var payload = ViettelPayloadMapper.Build(request, cfg);

        var response = await PostSubmitAsync(cfg, payload, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Viettel returned 401; refreshing token and retrying once.");
            _tokenStore.Invalidate();
            response = await PostSubmitAsync(cfg, payload, cancellationToken);
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Viettel submit HTTP {Code}: {Body}", (int)response.StatusCode, raw);
            return new TvanSubmissionResponse(
                Accepted: false,
                CqtCode: null,
                RejectReason: $"HTTP {(int)response.StatusCode}: {Truncate(raw, 512)}",
                Raw: raw);
        }

        return MapResponse(raw);
    }

    public async Task<TvanConnectionTestResult> TestConnectionAsync(ViettelOptions cfg, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        try
        {
            RequireConfigured(cfg);
            var (_, expiresAtUtc) = await LoginAsync(cfg, cancellationToken);
            return new TvanConnectionTestResult(
                true,
                $"Login succeeded. Token valid until approximately {expiresAtUtc:u}.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Viettel connection test failed.");
            return new TvanConnectionTestResult(false, ex.Message);
        }
    }

    // --- HTTP ---

    private async Task<HttpResponseMessage> PostSubmitAsync(ViettelOptions cfg, ViettelCreateCertRequest payload, CancellationToken ct)
    {
        var token = await _tokenStore.GetAsync(c => LoginAsync(cfg, c), cfg.TokenRefreshSkewSeconds, ct);
        var url = BuildUrl(cfg.BaseUrl, InjectMst(cfg.SubmitPath, cfg.SupplierTaxCode));

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        // Viettel uses a Cookie header, not Authorization: Bearer (per integration guide §5.5).
        request.Headers.TryAddWithoutValidation("Cookie", $"access_token={token}");
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        _logger.LogInformation("Viettel submit POST {Url}", url);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(cfg.TimeoutSeconds));
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
    }

    private async Task<(string Token, DateTime ExpiresAtUtc)> LoginAsync(ViettelOptions cfg, CancellationToken ct)
    {
        var url = BuildUrl(cfg.BaseUrl, cfg.LoginPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(
                new ViettelLoginRequest(cfg.Username, cfg.Password),
                options: JsonOptions)
        };

        _logger.LogInformation("Viettel login POST {Url} (user={User})", url, cfg.Username);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(cfg.TimeoutSeconds));
        using var response = await _httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ViettelLoginResponse>(JsonOptions, cts.Token)
            ?? throw new InvalidOperationException("Viettel login returned an empty payload.");
        if (string.IsNullOrWhiteSpace(payload.AccessToken))
            throw new InvalidOperationException("Viettel login response did not contain an access token.");

        // Lifetime: 30 min fallback. Viettel sometimes omits expires_in.
        var lifetime = payload.ExpiresIn > 0 ? payload.ExpiresIn : 30 * 60;
        return (payload.AccessToken!, DateTime.UtcNow.AddSeconds(lifetime));
    }

    // --- Response mapping (per integration guide §6.2 and §7 examples) ---

    private static TvanSubmissionResponse MapResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new TvanSubmissionResponse(false, null, "Empty response body.", raw);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Top level: { errorCode, description, result: { ... } }
            var errorCode = GetStringOrNull(root, "errorCode");
            var description = GetStringOrNull(root, "description");

            string? invoiceNo = null, transactionId = null, reservationCode = null, codeOfTax = null;
            if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object)
            {
                invoiceNo       = GetStringOrNull(result, "invoiceNo");
                transactionId   = GetStringOrNull(result, "transactionID") ?? GetStringOrNull(result, "transactionId");
                reservationCode = GetStringOrNull(result, "reservationCode");
                codeOfTax       = GetStringOrNull(result, "codeOfTax");
            }

            // Success when Viettel returns errorCode == null (per guide §7 examples).
            var ok = errorCode is null && !string.IsNullOrWhiteSpace(invoiceNo);

            // Surface the CQT code if present; otherwise fall back to reservationCode (lookup code).
            var cqt = !string.IsNullOrWhiteSpace(codeOfTax) ? codeOfTax : reservationCode;

            return ok
                ? new TvanSubmissionResponse(true, cqt, null, raw, invoiceNo, transactionId, reservationCode)
                : new TvanSubmissionResponse(false, cqt,
                    description ?? errorCode ?? "Viettel response did not indicate success.", raw,
                    invoiceNo, transactionId, reservationCode);
        }
        catch (JsonException)
        {
            return new TvanSubmissionResponse(false, null, "Viettel returned non-JSON payload.", raw);
        }
    }

    // --- Helpers ---

    private static void RequireConfigured(ViettelOptions cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl))
            throw new InvalidOperationException("Viettel BaseUrl is required. Configure it in the Viettel Service Settings page.");
        if (string.IsNullOrWhiteSpace(cfg.Username) || string.IsNullOrWhiteSpace(cfg.Password))
            throw new InvalidOperationException("Viettel username and password are required. Configure them in the Viettel Service Settings page.");
        if (string.IsNullOrWhiteSpace(cfg.SupplierTaxCode))
            throw new InvalidOperationException("Viettel SupplierTaxCode is required. Configure it in the Viettel Service Settings page.");
        if (string.IsNullOrWhiteSpace(cfg.InvoiceSeries))
            throw new InvalidOperationException("Viettel InvoiceSeries is required. Configure it in the Viettel Service Settings page.");
    }

    private static string BuildUrl(string baseUrl, string path)
    {
        var b = baseUrl.TrimEnd('/');
        var p = path.StartsWith('/') ? path : "/" + path;
        return b + p;
    }

    private static string InjectMst(string path, string? mst) =>
        string.IsNullOrWhiteSpace(mst) ? path : path.Replace("{mst}", mst, StringComparison.OrdinalIgnoreCase);

    private static string? GetStringOrNull(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        if (!root.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => v.ToString(),
            _ => null
        };
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "...";
}

internal sealed record ViettelLoginRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password);

internal sealed record ViettelLoginResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string? TokenType);
