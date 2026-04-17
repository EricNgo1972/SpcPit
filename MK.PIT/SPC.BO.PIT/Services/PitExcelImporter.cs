using System.Globalization;
using ClosedXML.Excel;
using Csla;

namespace SPC.BO.PIT;

/// <summary>
/// Reads the "Import form" sheet of a PIT Excel template. Row 5 holds canonical system field
/// names (the header); rows 6+ hold staff records. Produces row-level diagnostics with Excel
/// row + column coordinates so HR can jump straight to the offending cell.
/// </summary>
public sealed class PitExcelImporter
{
    public const string SheetName = "Import form";
    public const int HeaderRowNumber = 5;
    public const int FirstDataRowNumber = 6;

    /// <summary>Parse an xlsx stream into rows + diagnostics. No DB access.</summary>
    public PitImportResult Parse(Stream xlsxStream)
    {
        ArgumentNullException.ThrowIfNull(xlsxStream);
        var rows = new List<PitCertificateImportRow>();
        var diagnostics = new List<RowDiagnostic>();

        using var workbook = new XLWorkbook(xlsxStream);
        if (!workbook.TryGetWorksheet(SheetName, out var sheet))
        {
            diagnostics.Add(Diag(0, null, null, $"Sheet '{SheetName}' not found."));
            return new PitImportResult(rows, diagnostics);
        }

        var headerMap = BuildHeaderMap(sheet, diagnostics);
        if (headerMap.Count == 0)
            return new PitImportResult(rows, diagnostics);

        var usedRange = sheet.RangeUsed();
        if (usedRange is null)
            return new PitImportResult(rows, diagnostics);

        var lastRow = usedRange.LastRow().RowNumber();
        for (var r = FirstDataRowNumber; r <= lastRow; r++)
        {
            var rowAccessor = new RowAccessor(sheet, r, headerMap);
            if (rowAccessor.IsBlank)
                continue;

            var row = ParseRow(rowAccessor, diagnostics);
            if (row is not null)
                rows.Add(row);
        }

        return new PitImportResult(rows, diagnostics);
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet sheet, List<RowDiagnostic> diagnostics)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = sheet.Row(HeaderRowNumber);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var c = 1; c <= lastCol; c++)
        {
            var name = headerRow.Cell(c).GetString().Trim();
            if (string.IsNullOrEmpty(name)) continue;
            map[name] = c;
        }

        foreach (var required in new[] { "ProformaNo", "TaxPayerTaxCode", "TaxPayerName", "IncomePaymentYear" })
        {
            if (!map.ContainsKey(required))
                diagnostics.Add(Diag(
                    HeaderRowNumber, null, required,
                    $"Required header '{required}' not found in row {HeaderRowNumber}."));
        }
        return map;
    }

    private static PitCertificateImportRow? ParseRow(RowAccessor row, List<RowDiagnostic> diagnostics)
    {
        var rowNumber = row.RowNumber;
        var proforma = row.GetString("ProformaNo");
        var taxCode = row.GetString("TaxPayerTaxCode");
        var name = row.GetString("TaxPayerName");
        var year = row.GetInt("IncomePaymentYear", diagnostics);

        if (string.IsNullOrWhiteSpace(proforma))
            diagnostics.Add(Diag(rowNumber, row.ColumnOf("ProformaNo"), "ProformaNo", "ProformaNo is required."));
        if (string.IsNullOrWhiteSpace(taxCode))
            diagnostics.Add(Diag(rowNumber, row.ColumnOf("TaxPayerTaxCode"), "TaxPayerTaxCode", "TaxPayerTaxCode is required."));
        else if (taxCode!.Length is not (10 or 13) || !taxCode.All(char.IsDigit))
            diagnostics.Add(Diag(rowNumber, row.ColumnOf("TaxPayerTaxCode"), "TaxPayerTaxCode", "TaxPayerTaxCode must be 10 or 13 digits."));
        if (string.IsNullOrWhiteSpace(name))
            diagnostics.Add(Diag(rowNumber, row.ColumnOf("TaxPayerName"), "TaxPayerName", "TaxPayerName is required."));

        var from = row.GetNullableInt("IncomePaymentMonthFrom", diagnostics);
        var to = row.GetNullableInt("IncomePaymentMonthTo", diagnostics);
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            diagnostics.Add(Diag(rowNumber, row.ColumnOf("IncomePaymentMonthFrom"), "IncomePaymentMonthFrom",
                "IncomePaymentMonthFrom must be ≤ IncomePaymentMonthTo."));

        return new PitCertificateImportRow
        {
            ImportRowNumber = rowNumber,
            TaxPayerCode = row.GetString("TaxPayerCode"),
            ProformaNo = proforma,
            TaxPayerTaxCode = taxCode,
            TaxPayerName = name,
            Nationality = row.GetOptionalString("Nationality"),
            ResidentType = row.GetOptionalString("ResidentType"),
            IdentificationNo = row.GetOptionalString("IdentificationNo"),
            IssueDate = row.GetNullableDate("IssueDate", diagnostics),
            IssuePlace = row.GetOptionalString("IssuePlace"),
            Phone = row.GetOptionalString("Phone"),
            Email = row.GetOptionalString("Email"),
            Address = row.GetOptionalString("Address"),
            InsurancePremiums = row.GetNullableDecimal("InsurancePremiums", diagnostics),
            CharityDonations = row.GetNullableDecimal("CharityDonations", diagnostics),
            IncomePaymentMonthFrom = from,
            IncomePaymentMonthTo = to,
            IncomePaymentYear = year,
            TotalTaxableIncome = row.GetDecimal("TotalTaxableIncome", diagnostics),
            AmountPersonalIncomeTax = row.GetDecimal("AmountPersonalIncomeTax", diagnostics),
            IncomeStillReceivable = row.GetNullableDecimal("IncomeStillReceivable", diagnostics),
            IncomeType = row.GetOptionalString("IncomeType"),
            Note = row.GetOptionalString("Note"),
            RelatedProformaNo = row.GetOptionalString("RelatedProformaNo"),
            RelatedFormNo = row.GetOptionalString("RelatedFormNo")
        };
    }

    /// <summary>Insert parsed rows as PitCertificate records via CSLA. Caller is responsible for
    /// only calling this with error-free rows (check <see cref="PitImportResult.HasErrors"/>).</summary>
    public async Task<int> CommitAsync(ApplicationContext ctx, IReadOnlyList<PitCertificateImportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var factory = ctx.GetRequiredService<IDataPortalFactory>();
        var portal = factory.GetPortal<PitCertificate>();

        var inserted = 0;
        foreach (var r in rows)
        {
            var cert = await portal.CreateAsync();
            cert.TaxPayerCode = r.TaxPayerCode;
            cert.ProformaNo = r.ProformaNo;
            cert.TaxPayerTaxCode = r.TaxPayerTaxCode;
            cert.TaxPayerName = r.TaxPayerName;
            cert.Nationality = r.Nationality;
            cert.ResidentType = r.ResidentType;
            cert.IdentificationNo = r.IdentificationNo;
            cert.IssueDate = r.IssueDate;
            cert.IssuePlace = r.IssuePlace;
            cert.Phone = r.Phone;
            cert.Email = r.Email;
            cert.Address = r.Address;
            cert.InsurancePremiums = r.InsurancePremiums;
            cert.CharityDonations = r.CharityDonations;
            cert.IncomePaymentMonthFrom = r.IncomePaymentMonthFrom;
            cert.IncomePaymentMonthTo = r.IncomePaymentMonthTo;
            cert.IncomePaymentYear = r.IncomePaymentYear;
            cert.TotalTaxableIncome = r.TotalTaxableIncome;
            cert.AmountPersonalIncomeTax = r.AmountPersonalIncomeTax;
            cert.IncomeStillReceivable = r.IncomeStillReceivable;
            cert.IncomeType = r.IncomeType;
            cert.Note = r.Note;
            cert.RelatedProformaNo = r.RelatedProformaNo;
            cert.RelatedFormNo = r.RelatedFormNo;
            cert.ImportRowNumber = r.ImportRowNumber;
            cert.UpdatedAt = DateTime.UtcNow;
            cert = await cert.SaveAsync();
            inserted++;
        }
        return inserted;
    }

    /// <summary>Factory for diagnostics: derives Excel-style letter from the 1-based column.</summary>
    private static RowDiagnostic Diag(int rowNumber, int? columnNumber, string? field, string message,
        ImportSeverity severity = ImportSeverity.Error) =>
        new(rowNumber, columnNumber, ToColumnLetter(columnNumber), field, message, severity);

    /// <summary>Converts 1-based column number to Excel letter (1→A, 27→AA). Null/zero → null.</summary>
    public static string? ToColumnLetter(int? column)
    {
        if (!column.HasValue || column.Value <= 0) return null;
        var n = column.Value;
        var letter = string.Empty;
        while (n > 0)
        {
            var rem = (n - 1) % 26;
            letter = (char)('A' + rem) + letter;
            n = (n - 1) / 26;
        }
        return letter;
    }

    private sealed class RowAccessor(IXLWorksheet sheet, int rowNumber, Dictionary<string, int> headerMap)
    {
        public int RowNumber { get; } = rowNumber;

        /// <summary>1-based Excel column for a header, or null if header isn't present.</summary>
        public int? ColumnOf(string header) =>
            headerMap.TryGetValue(header, out var col) ? col : null;

        public bool IsBlank
        {
            get
            {
                foreach (var col in headerMap.Values)
                {
                    var v = sheet.Cell(rowNumber, col).GetString();
                    if (!string.IsNullOrWhiteSpace(v)) return false;
                }
                return true;
            }
        }

        private IXLCell? Cell(string header) =>
            headerMap.TryGetValue(header, out var col) ? sheet.Cell(rowNumber, col) : null;

        public string GetString(string header) =>
            Cell(header)?.GetString().Trim() ?? string.Empty;

        public string? GetOptionalString(string header)
        {
            var v = GetString(header);
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        public int GetInt(string header, List<RowDiagnostic> diagnostics)
        {
            var v = GetNullableInt(header, diagnostics);
            if (!v.HasValue)
            {
                diagnostics.Add(Diag(rowNumber, ColumnOf(header), header, $"{header} is required."));
                return 0;
            }
            return v.Value;
        }

        public int? GetNullableInt(string header, List<RowDiagnostic> diagnostics)
        {
            var cell = Cell(header);
            if (cell is null || cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.Number) return (int)cell.GetDouble();
            var raw = cell.GetString().Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return n;
            diagnostics.Add(Diag(rowNumber, ColumnOf(header), header, $"'{raw}' is not a valid integer."));
            return null;
        }

        public decimal GetDecimal(string header, List<RowDiagnostic> diagnostics)
        {
            var v = GetNullableDecimal(header, diagnostics);
            return v ?? 0m;
        }

        public decimal? GetNullableDecimal(string header, List<RowDiagnostic> diagnostics)
        {
            var cell = Cell(header);
            if (cell is null || cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
            var raw = cell.GetString().Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            diagnostics.Add(Diag(rowNumber, ColumnOf(header), header, $"'{raw}' is not a valid decimal."));
            return null;
        }

        public DateTime? GetNullableDate(string header, List<RowDiagnostic> diagnostics)
        {
            var cell = Cell(header);
            if (cell is null || cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
            var raw = cell.GetString().Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) return dt;
            diagnostics.Add(Diag(rowNumber, ColumnOf(header), header, $"'{raw}' is not a valid date."));
            return null;
        }
    }
}
