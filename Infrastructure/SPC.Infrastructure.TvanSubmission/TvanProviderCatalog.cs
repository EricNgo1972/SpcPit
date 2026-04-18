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
        NoneCode, "(không chọn - chỉ lưu nội bộ)",
        "Không gửi qua T-VAN. Chứng từ chỉ được lưu trong ứng dụng.",
        IsAvailable: true, RequiresSignedXml: false);

    public static readonly TvanProviderInfo Stub = new(
        StubCode, "Stub (kiểm thử)",
        "Mô phỏng nội bộ. Trả về số chứng từ và mã CQT giả lập. Dùng cho phát triển và kiểm thử.",
        IsAvailable: true, RequiresSignedXml: false);

    public static readonly TvanProviderInfo Viettel = new(
        ViettelCode, "Viettel T-VAN",
        "Quy trình Viettel SInvoice CTS Server. Nhận JSON có cấu trúc; Viettel tự ký nội bộ.",
        IsAvailable: true, RequiresSignedXml: false);

    public static readonly TvanProviderInfo Vnpt = new(
        VnptCode, "VNPT eInvoice",
        "Cổng kết nối VNPT-Invoice. Dự kiến - chưa hỗ trợ.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo Misa = new(
        MisaCode, "MISA meInvoice",
        "Cổng kết nối MISA meInvoice. Dự kiến - chưa hỗ trợ.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo SoftDreams = new(
        SoftDreamsCode, "SoftDreams EasyInvoice",
        "Cổng kết nối SoftDreams EasyInvoice / SoftDreams PIT. Dự kiến - chưa hỗ trợ.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo MInvoice = new(
        MInvoiceCode, "FPT M-Invoice",
        "Cổng kết nối FPT M-Invoice. Dự kiến - chưa hỗ trợ.",
        IsAvailable: false, RequiresSignedXml: true);

    public static readonly TvanProviderInfo Bkav = new(
        BkavCode, "BKAV eHoaDon",
        "Cổng kết nối BKAV eHoaDon. Dự kiến - chưa hỗ trợ.",
        IsAvailable: false, RequiresSignedXml: true);

    /// <summary>All providers in UI-display order (available first, then planned, then "none").</summary>
    public static readonly IReadOnlyList<TvanProviderInfo> All =
        new[] { Stub, Viettel, Vnpt, Misa, SoftDreams, MInvoice, Bkav, None };

    /// <summary>Look up a provider by its code; unknown codes fall back to <see cref="Stub"/>.</summary>
    public static TvanProviderInfo Get(string? code) =>
        All.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)) ?? Stub;
}
