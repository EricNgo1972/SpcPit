namespace SPC.Infrastructure.TvanSubmission;

/// <summary>Capabilities and display metadata for one T-VAN provider.</summary>
public sealed record TvanProviderInfo(
    string Code,
    string DisplayName,
    string Description,
    bool IsAvailable,
    bool RequiresSignedXml);

/// <summary>
/// Catalog of T-VAN providers the app knows about. Drives the dropdown in PitSettings and
/// the dispatcher's routing. Only <see cref="Stub"/> and <see cref="Viettel"/> have real
/// adapters today; the others are listed so users can see the roadmap.
/// </summary>
public static class TvanProviderCatalog
{
    public const string NoneCode    = "None";
    public const string StubCode    = "Stub";
    public const string ViettelCode = "Viettel";
    public const string VnptCode    = "VNPT";
    public const string MisaCode    = "MISA";
    public const string SoftDreamsCode = "SoftDreams";
    public const string MInvoiceCode = "M-Invoice";
    public const string BkavCode    = "BKAV";

    public static readonly TvanProviderInfo None = new(
        NoneCode, "(none — save locally only)",
        "No T-VAN submission. Certificates live only in this app.",
        IsAvailable: true, RequiresSignedXml: false);

    public static readonly TvanProviderInfo Stub = new(
        StubCode, "Stub (testing)",
        "In-process fake. Returns synthetic invoice numbers and CQT codes. Use during development.",
        IsAvailable: true, RequiresSignedXml: false);

    public static readonly TvanProviderInfo Viettel = new(
        ViettelCode, "Viettel T-VAN",
        "Viettel SInvoice CTS Server flow. Takes structured JSON; Viettel signs internally.",
        IsAvailable: true, RequiresSignedXml: false);

    public static readonly TvanProviderInfo Vnpt = new(
        VnptCode, "VNPT eInvoice",
        "VNPT-Invoice gateway. Planned — not implemented.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo Misa = new(
        MisaCode, "MISA meInvoice",
        "MISA meInvoice gateway. Planned — not implemented.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo SoftDreams = new(
        SoftDreamsCode, "SoftDreams EasyInvoice",
        "SoftDreams EasyInvoice / Softdreams PIT module. Planned — not implemented.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo MInvoice = new(
        MInvoiceCode, "FPT M-Invoice",
        "FPT M-Invoice gateway. Planned — not implemented.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo Bkav = new(
        BkavCode, "BKAV eHoaDon",
        "BKAV eHoaDon gateway. Planned — not implemented.",
        IsAvailable: false, RequiresSignedXml: true);

    /// <summary>All providers in UI-display order (available first, then planned, then "none").</summary>
    public static readonly IReadOnlyList<TvanProviderInfo> All =
        new[] { Stub, Viettel, Vnpt, Misa, SoftDreams, MInvoice, Bkav, None };

    /// <summary>Look up a provider by its code; unknown codes fall back to <see cref="Stub"/>.</summary>
    public static TvanProviderInfo Get(string? code) =>
        All.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)) ?? Stub;
}
