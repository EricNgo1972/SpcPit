namespace SPC.Infrastructure.TvanSubmission;

/// <summary>Startup-time switch for which T-VAN provider is active.</summary>
public sealed class TvanSubmissionOptions
{
    /// <summary>"Stub" (in-process fake) or a real provider name ("Viettel", "VNPT", "MISA", …).</summary>
    public string Mode { get; set; } = "Stub";
}

/// <summary>
/// Runtime Viettel connection settings. This POCO is the adapter's own contract;
/// the Blazor UI stores/edits them via <c>SPC.BO.PIT.ViettelSettings</c> (KeyValueStore)
/// and the adapter builds a fresh instance per submission from the BO snapshot.
/// </summary>
public sealed class ViettelOptions
{
    public string BaseUrl { get; set; } = "https://api-sinvoice.viettel.vn";
    public string LoginPath { get; set; } = "/auth/login";
    public string SubmitPath { get; set; } = "/api/InvoiceAPI/InvoiceWS/createTaxDeductionCertificate/{mst}";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string SupplierTaxCode { get; set; } = "";
    public string InvoiceSeries { get; set; } = "CT/25E";
    public string InvoiceType { get; set; } = "03/TNCN";
    public string TemplateCode { get; set; } = "03/TNCN";
    public string CurrencyCode { get; set; } = "VND";
    public int TokenRefreshSkewSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed record TvanConnectionTestResult(
    bool Success,
    string Message);
