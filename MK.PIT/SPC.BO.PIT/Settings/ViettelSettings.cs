using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Csla;
using SPC.BO;
using SPC.DAL;

namespace SPC.BO.PIT;

/// <summary>
/// Connection settings for Viettel T-VAN (CTS Server flow). Stored in KeyValueStore under
/// category "ViettelSettings" so credentials and endpoints can be edited in the app without
/// redeploying appsettings.json.
/// </summary>
[Serializable]
public class ViettelSettings : KeyValueSettingsBase<ViettelSettings>
{
    public static Task<ViettelSettings> GetViettelSettingsAsync(ApplicationContext ctx) => GetAsync(ctx);

    protected override void SetDefaultValues()
    {
        base.SetDefaultValues();
        BaseUrl = "https://api-sinvoice.viettel.vn";
        LoginPath = "/auth/login";
        SubmitPath = "/api/InvoiceAPI/InvoiceWS/createTaxDeductionCertificate/{mst}";
        InvoiceSeries = "CT/25E";
        InvoiceType = "03/TNCN";
        TemplateCode = "03/TNCN";
        CurrencyCode = "VND";
        TokenRefreshSkewSeconds = 60;
        TimeoutSeconds = 30;
        ConnectionReady = false;
    }

    // --- Endpoint ---

    public static readonly PropertyInfo<string> BaseUrlProperty = RegisterProperty<string>(nameof(BaseUrl));
    public string BaseUrl { get => GetProperty(BaseUrlProperty); set => SetProperty(BaseUrlProperty, value); }

    public static readonly PropertyInfo<string> LoginPathProperty = RegisterProperty<string>(nameof(LoginPath));
    public string LoginPath { get => GetProperty(LoginPathProperty); set => SetProperty(LoginPathProperty, value); }

    public static readonly PropertyInfo<string> SubmitPathProperty = RegisterProperty<string>(nameof(SubmitPath));
    public string SubmitPath { get => GetProperty(SubmitPathProperty); set => SetProperty(SubmitPathProperty, value); }

    // --- Credentials ---

    public static readonly PropertyInfo<string> UsernameProperty = RegisterProperty<string>(nameof(Username));
    public string Username { get => GetProperty(UsernameProperty); set => SetProperty(UsernameProperty, value); }

    public static readonly PropertyInfo<string> PasswordProperty = RegisterProperty<string>(nameof(Password));
    public string Password { get => GetProperty(PasswordProperty); set => SetProperty(PasswordProperty, value); }

    public static readonly PropertyInfo<string> SupplierTaxCodeProperty = RegisterProperty<string>(nameof(SupplierTaxCode));
    public string SupplierTaxCode { get => GetProperty(SupplierTaxCodeProperty); set => SetProperty(SupplierTaxCodeProperty, value); }

    // --- Cert product parameters ---

    public static readonly PropertyInfo<string> InvoiceSeriesProperty = RegisterProperty<string>(nameof(InvoiceSeries));
    public string InvoiceSeries { get => GetProperty(InvoiceSeriesProperty); set => SetProperty(InvoiceSeriesProperty, value); }

    public static readonly PropertyInfo<string> InvoiceTypeProperty = RegisterProperty<string>(nameof(InvoiceType));
    public string InvoiceType { get => GetProperty(InvoiceTypeProperty); set => SetProperty(InvoiceTypeProperty, value); }

    public static readonly PropertyInfo<string> TemplateCodeProperty = RegisterProperty<string>(nameof(TemplateCode));
    public string TemplateCode { get => GetProperty(TemplateCodeProperty); set => SetProperty(TemplateCodeProperty, value); }

    public static readonly PropertyInfo<string> CurrencyCodeProperty = RegisterProperty<string>(nameof(CurrencyCode));
    public string CurrencyCode { get => GetProperty(CurrencyCodeProperty); set => SetProperty(CurrencyCodeProperty, value); }

    // --- Tuning ---

    public static readonly PropertyInfo<int> TokenRefreshSkewSecondsProperty = RegisterProperty<int>(nameof(TokenRefreshSkewSeconds));
    public int TokenRefreshSkewSeconds { get => GetProperty(TokenRefreshSkewSecondsProperty); set => SetProperty(TokenRefreshSkewSecondsProperty, value); }

    public static readonly PropertyInfo<int> TimeoutSecondsProperty = RegisterProperty<int>(nameof(TimeoutSeconds));
    public int TimeoutSeconds { get => GetProperty(TimeoutSecondsProperty); set => SetProperty(TimeoutSecondsProperty, value); }

    // --- Readiness / connection test state ---

    public static readonly PropertyInfo<bool> ConnectionReadyProperty = RegisterProperty<bool>(nameof(ConnectionReady));
    public bool ConnectionReady { get => GetProperty(ConnectionReadyProperty); set => SetProperty(ConnectionReadyProperty, value); }

    public static readonly PropertyInfo<DateTime?> LastConnectionTestUtcProperty = RegisterProperty<DateTime?>(nameof(LastConnectionTestUtc));
    public DateTime? LastConnectionTestUtc { get => GetProperty(LastConnectionTestUtcProperty); set => SetProperty(LastConnectionTestUtcProperty, value); }

    public static readonly PropertyInfo<string> LastConnectionTestMessageProperty = RegisterProperty<string>(nameof(LastConnectionTestMessage));
    public string LastConnectionTestMessage { get => GetProperty(LastConnectionTestMessageProperty); set => SetProperty(LastConnectionTestMessageProperty, value); }

    public static readonly PropertyInfo<string> LastTestedConfigHashProperty = RegisterProperty<string>(nameof(LastTestedConfigHash));
    public string LastTestedConfigHash { get => GetProperty(LastTestedConfigHashProperty); set => SetProperty(LastTestedConfigHashProperty, value); }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsReadyForSubmission =>
        ConnectionReady &&
        string.Equals(LastTestedConfigHash, ComputeConnectionConfigHash(), StringComparison.Ordinal);

    public string ComputeConnectionConfigHash()
    {
        var raw = string.Join("\n",
            BaseUrl?.Trim() ?? string.Empty,
            LoginPath?.Trim() ?? string.Empty,
            SubmitPath?.Trim() ?? string.Empty,
            Username?.Trim() ?? string.Empty,
            Password ?? string.Empty,
            SupplierTaxCode?.Trim() ?? string.Empty,
            InvoiceSeries?.Trim() ?? string.Empty,
            InvoiceType?.Trim() ?? string.Empty,
            TemplateCode?.Trim() ?? string.Empty,
            CurrencyCode?.Trim() ?? string.Empty,
            TokenRefreshSkewSeconds.ToString(),
            TimeoutSeconds.ToString());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    // --- Encryption-at-rest for sensitive fields (Password) ----------------------------
    // Encrypted values are stored with a versioned prefix so we can detect/round-trip them
    // cleanly and tolerate old plaintext rows during a one-time upgrade.
    private const string EncryptedPrefix = "enc:v1:";

    private static bool IsSecret(string propertyName) => propertyName == nameof(Password);

    /// <summary>Serialize to the DAL — encrypt secrets before they leave the BO.</summary>
    protected override DTO<ViettelSettings> ToDto()
    {
        var dto = new DTO<ViettelSettings>();
        var protector = ApplicationContext.GetRequiredService<ISensitiveDataProtector>();

        foreach (var prop in FieldManager.GetRegisteredProperties())
        {
            var value = ReadProperty(prop);

            if (IsSecret(prop.Name) && value is string plain && !string.IsNullOrEmpty(plain)
                && !plain.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            {
                value = EncryptedPrefix + protector.Protect(plain);
            }

            dto.Set(prop.Name, value);
        }
        return dto;
    }

    // --- FromDto override ---------------------------------------------------------------
    // Responsibilities:
    //   1. Decrypt secret fields (Password) after reading them from the DAL.
    //   2. Work around a CSLA-level corner case in KeyValueSettingsBase<T>.FromDto: values
    //      persisted as bare all-digit strings (e.g. SupplierTaxCode "0100109106") round-trip
    //      as JsonElement Number via DTO<T>.FromStringDictionary, and the default converter
    //      then throws "Cannot get the value of a token type 'Number' as a string." We read
    //      the element's raw text instead when the target property is string-typed.

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    protected override void FromDto(DTO<ViettelSettings> dto)
    {
        var values = dto.ToDictionary();
        if (values is null || values.Count == 0)
        {
            MarkNew();
            return;
        }

        var protector = ApplicationContext.GetRequiredService<ISensitiveDataProtector>();

        foreach (var prop in FieldManager.GetRegisteredProperties())
        {
            if (!values.TryGetValue(prop.Name, out var raw) || raw is null)
                continue;

            if (raw is string s && string.IsNullOrEmpty(s))
            {
                LoadProperty(prop, null);
                continue;
            }

            var targetType = prop.Type;
            object? converted;

            if (raw is JsonElement element)
            {
                converted = targetType == typeof(string)
                    ? (element.ValueKind == JsonValueKind.String
                        ? element.GetString() ?? string.Empty
                        : element.ToString())
                    : JsonSerializer.Deserialize(element.GetRawText(), targetType, s_jsonOptions);
            }
            else if (raw.GetType() == targetType)
            {
                converted = raw;
            }
            else
            {
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
                converted = Convert.ChangeType(raw, underlying);
            }

            // Decrypt secrets that carry the versioned prefix. Legacy plaintext values
            // (saved before encryption was enabled) are loaded as-is and re-encrypted the
            // next time the user saves.
            if (IsSecret(prop.Name) && converted is string cipher && cipher.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            {
                try
                {
                    converted = protector.Unprotect(cipher[EncryptedPrefix.Length..]);
                }
                catch
                {
                    // Tampered or key-ring mismatch — surface as a clear error rather than a
                    // silent empty password. The user will have to re-enter it on the page.
                    converted = string.Empty;
                }
            }

            LoadProperty(prop, converted);
        }
    }
}
