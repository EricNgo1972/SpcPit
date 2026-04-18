using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SPC.BO.PIT.Xml;

namespace SPC.BO.PIT.Pdf;

public sealed record PitPdfBuildContext(
    PitCertificateXmlInput Certificate,
    PitSettingsXmlInput Settings,
    string? CqtCode,
    string? FormNumber,     // "Mẫu số"   — falls back to PitSettings.XmlMessageTypeCode
    string? Series,         // "Ký hiệu"  — e.g. "1/2025/T"
    DateTime? IssuedOn);    // footer date; defaults to today (GMT+7)

/// <summary>
/// Renders the official Vietnamese Personal Income Tax withholding certificate
/// (Chứng từ khấu trừ thuế thu nhập cá nhân) as a printable A4 PDF. Layout mirrors
/// the government template: republic header → certificate meta (top-right) → three
/// numbered sections (issuer / taxpayer / withholding) → signature block.
/// </summary>
public sealed class PitPdfBuilder
{
    private static readonly object QuestPdfInitLock = new();
    private static bool _questPdfInitialized;

    public byte[] Build(PitPdfBuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        EnsureQuestPdfInitialized();
        return Document.Create(container => Compose(container, ctx)).GeneratePdf();
    }

    private static void EnsureQuestPdfInitialized()
    {
        if (_questPdfInitialized)
            return;

        lock (QuestPdfInitLock)
        {
            if (_questPdfInitialized)
                return;

            QuestPDF.Settings.License = LicenseType.Community;
            _questPdfInitialized = true;
        }
    }

    private static void Compose(IDocumentContainer container, PitPdfBuildContext ctx)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.8f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10));
            page.Content().Element(content => Body(content, ctx));
        });
    }

    private static void Body(IContainer container, PitPdfBuildContext ctx)
    {
        var cert = ctx.Certificate;
        var s = ctx.Settings;
        var issuedOn = ctx.IssuedOn ?? DateTime.UtcNow.AddHours(7);  // GMT+7
        var formNumber = !string.IsNullOrWhiteSpace(ctx.FormNumber) ? ctx.FormNumber : s.XmlMessageTypeCode;
        var series = ctx.Series ?? string.Empty;
        var sequence = cert.ProformaNo;

        container.Column(col =>
        {
            col.Spacing(8);

            // --- Header: Republic text + certificate meta ---
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold();
                    left.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Italic();
                    left.Item().AlignCenter().Text("SOCIALIST REPUBLIC OF VIETNAM").FontSize(9);
                    left.Item().AlignCenter().Text("Independence - Freedom - Happiness").FontSize(9).Italic();
                });

                row.ConstantItem(175).Column(right =>
                {
                    right.Item().Text(t => { t.Span("Mẫu số/").Italic(); t.Span("Form no.: ").Italic(); t.Span(formNumber).Bold(); });
                    right.Item().Text(t => { t.Span("Ký hiệu/").Italic(); t.Span("Serial: ").Italic(); t.Span(series).Bold(); });
                    right.Item().Text(t => { t.Span("Số/").Italic(); t.Span("No.: ").Italic(); t.Span(sequence).Bold(); });
                    if (!string.IsNullOrWhiteSpace(ctx.CqtCode))
                        right.Item().Text(t => { t.Span("Mã CQT: ").Italic(); t.Span(ctx.CqtCode!).Bold().FontColor(Colors.Green.Darken2); });
                });
            });

            // --- Title ---
            col.Item().PaddingTop(6).AlignCenter().Text("CHỨNG TỪ KHẤU TRỪ THUẾ THU NHẬP CÁ NHÂN").FontSize(14).Bold();
            col.Item().AlignCenter().Text("CERTIFICATE OF PERSONAL INCOME TAX WITHHOLDING").FontSize(11).Italic();

            // --- Section I: issuer ---
            col.Item().PaddingTop(10).Text("I. THÔNG TIN TỔ CHỨC TRẢ THU NHẬP").Bold();
            col.Item().Text("(Information of the income paying organization)").Italic().FontSize(9);
            col.Item().Column(sec =>
            {
                Field(sec, "01", "Tên tổ chức trả thu nhập", "Name of the income paying organization", s.OrganizationName);
                Field(sec, "02", "Mã số thuế", "Tax identification number",                             s.OrganizationTaxCode);
                Field(sec, "03", "Địa chỉ",   "Address",                                                s.OrganizationAddress ?? "");
                Field(sec, "04", "Điện thoại","Phone",                                                  s.OrganizationPhone ?? "");
            });

            // --- Section II: taxpayer ---
            col.Item().PaddingTop(6).Text("II. THÔNG TIN CÁ NHÂN NHẬN THU NHẬP").Bold();
            col.Item().Text("(Information of the taxpayer)").Italic().FontSize(9);
            col.Item().Column(sec =>
            {
                Field(sec, "05", "Họ và tên", "Full name", cert.TaxPayerName);
                Field(sec, "06", "Mã số thuế","Tax identification number", cert.TaxPayerTaxCode);
                Field(sec, "07", "Quốc tịch", "Nationality", cert.Nationality ?? "");
                sec.Item().PaddingTop(2).Row(r =>
                {
                    r.ConstantItem(30).Text("[08]").SemiBold();
                    r.RelativeItem().Text(t =>
                    {
                        var resident = cert.ResidentType == "00081";
                        t.Span(resident ? "☒ " : "☐ "); t.Span("Cá nhân cư trú ").Bold(); t.Span("(Resident individual)    ").Italic();
                        t.Span(resident ? "☐ " : "☒ "); t.Span("Cá nhân không cư trú ").Bold(); t.Span("(Non-resident individual)").Italic();
                    });
                });
                Field(sec, "09", "Số CMND/CCCD/Hộ chiếu", "ID / passport number",
                    FormatIdBlock(cert.IdentificationNo, cert.IssueDate, cert.IssuePlace));
                Field(sec, "10", "Địa chỉ",  "Address", cert.Address ?? "");
                Field(sec, "11", "Điện thoại","Phone",  cert.Phone ?? "");
                Field(sec, "12", "Email",     "Email",   cert.Email ?? "");
            });

            // --- Section III: withholding ---
            col.Item().PaddingTop(6).Text("III. THÔNG TIN THUẾ THU NHẬP CÁ NHÂN ĐÃ KHẤU TRỪ").Bold();
            col.Item().Text("(Information of personal income tax withheld)").Italic().FontSize(9);
            col.Item().Column(sec =>
            {
                Field(sec, "13", "Khoản thu nhập",              "Type of income paid", cert.IncomeType ?? "");
                Field(sec, "14", "Thời điểm trả thu nhập",      "Date of income payment",
                    FormatPeriod(cert.IncomePaymentMonthFrom, cert.IncomePaymentMonthTo, cert.IncomePaymentYear));
                Field(sec, "15", "Tổng thu nhập chịu thuế đã trả", "Total taxable income (VND)",
                    FormatMoney(cert.TotalTaxableIncome));
                Field(sec, "16", "Tổng các khoản giảm trừ (BHBB + đóng góp)", "Total deductions (compulsory insurance + donations) (VND)",
                    FormatMoney((cert.InsurancePremiums ?? 0m) + (cert.CharityDonations ?? 0m)));
                Field(sec, "17", "Số thuế TNCN đã khấu trừ",   "PIT withheld (VND)",
                    FormatMoney(cert.AmountPersonalIncomeTax));
                Field(sec, "18", "Số thu nhập còn được nhận",   "Remaining net income (VND)",
                    FormatMoney(cert.IncomeStillReceivable));
            });

            // --- Footer / signature block ---
            col.Item().PaddingTop(16).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(240).Column(right =>
                {
                    right.Item().AlignCenter().Text(t =>
                    {
                        t.Span($"..........., ngày {issuedOn.Day:D2} tháng {issuedOn.Month:D2} năm {issuedOn.Year}").Italic();
                    });
                    right.Item().AlignCenter().Text($"({FormatEnglishDate(issuedOn)})").FontSize(9).Italic();
                    right.Item().PaddingTop(4).AlignCenter().Text("ĐẠI DIỆN TỔ CHỨC KHẤU TRỪ").Bold();
                    right.Item().AlignCenter().Text("(Representative of the tax withholding organization)").Italic().FontSize(9);
                    right.Item().Height(60);  // signature space
                    if (!string.IsNullOrWhiteSpace(s.OrganizationName))
                        right.Item().AlignCenter().Text(s.OrganizationName).Bold();
                });
            });
        });
    }

    // --- helpers ---

    private static void Field(ColumnDescriptor col, string number, string viLabel, string enLabel, string value)
    {
        col.Item().PaddingTop(2).Row(row =>
        {
            row.ConstantItem(30).Text($"[{number}]").SemiBold();
            row.RelativeItem().Text(t =>
            {
                t.Span($"{viLabel} ").Bold();
                t.Span($"({enLabel}): ").Italic().FontSize(9);
                t.Span(string.IsNullOrWhiteSpace(value) ? "......................" : value);
            });
        });
    }

    private static string FormatIdBlock(string? idNo, DateTime? issueDate, string? issuePlace)
    {
        if (string.IsNullOrWhiteSpace(idNo)) return "";
        var parts = new List<string> { idNo! };
        if (issueDate.HasValue)
            parts.Add($"Ngày cấp: {issueDate.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(issuePlace))
            parts.Add($"Nơi cấp: {issuePlace}");
        return string.Join(" · ", parts);
    }

    private static string FormatPeriod(int? from, int? to, int year)
    {
        if (from.HasValue && to.HasValue)
            return $"Từ tháng {from:D2} đến tháng {to:D2} năm {year}";
        if (from.HasValue) return $"Tháng {from:D2}/{year}";
        return $"Năm {year}";
    }

    private static string FormatMoney(decimal? value)
    {
        if (!value.HasValue || value.Value == 0m) return "0";
        return value.Value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatEnglishDate(DateTime d) =>
        d.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
}
