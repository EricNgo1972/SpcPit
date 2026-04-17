using ClosedXML.Excel;
using FluentAssertions;
using SPC.BO.PIT;
using Xunit;

namespace SPC.Tests.PIT.Commands;

public class PitExcelImporterTests
{
    [Fact]
    public void Parse_returns_rows_and_diagnostics_for_mixed_input()
    {
        using var stream = BuildFixtureXlsx();
        var importer = new PitExcelImporter();
        var result = importer.Parse(stream);

        // Expect 2 valid rows and 1 row with a bad MonthFrom > MonthTo.
        result.Rows.Should().HaveCount(3);
        result.Diagnostics.Should().ContainSingle(d =>
            d.Field == "IncomePaymentMonthFrom" && d.Severity == ImportSeverity.Error);
        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Parse_flags_missing_taxcode_with_cell_coordinates()
    {
        using var stream = BuildFixtureXlsx();
        var importer = new PitExcelImporter();
        var result = importer.Parse(stream);

        var diag = result.Diagnostics.SingleOrDefault(d =>
            d.Field == "TaxPayerTaxCode" && d.Severity == ImportSeverity.Error);
        diag.Should().NotBeNull();
        diag!.RowNumber.Should().Be(7, "the tax-code-less row is the second data row (Excel row 7).");
        diag.ColumnNumber.Should().Be(3, "TaxPayerTaxCode is the third header column in the fixture.");
        diag.ColumnLetter.Should().Be("C");
        diag.CellRef.Should().Be("C7");
    }

    [Theory]
    [InlineData(1, "A")]
    [InlineData(3, "C")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(52, "AZ")]
    [InlineData(53, "BA")]
    public void Column_letter_matches_Excel_naming(int column, string expected)
    {
        PitExcelImporter.ToColumnLetter(column).Should().Be(expected);
    }

    [Fact]
    public void Parse_reports_error_when_sheet_missing()
    {
        using var mem = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            wb.Worksheets.Add("Other");
            wb.SaveAs(mem);
        }
        mem.Position = 0;

        var result = new PitExcelImporter().Parse(mem);
        result.Rows.Should().BeEmpty();
        result.Diagnostics.Should().ContainSingle(d => d.Message.Contains("Import form"));
    }

    [Fact]
    public void Parse_skips_blank_rows_between_data()
    {
        using var mem = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var sheet = wb.Worksheets.Add(PitExcelImporter.SheetName);
            WriteHeaderRow(sheet);
            WriteDataRow(sheet, 6, "EMP1", "0000001", "0123456789", "A", year: 2025);
            // row 7 intentionally blank
            WriteDataRow(sheet, 8, "EMP2", "0000002", "9876543210", "B", year: 2025);
            wb.SaveAs(mem);
        }
        mem.Position = 0;

        var result = new PitExcelImporter().Parse(mem);
        result.Rows.Should().HaveCount(2);
        result.Diagnostics.Should().BeEmpty();
    }

    // --- Helpers ---

    private static MemoryStream BuildFixtureXlsx()
    {
        var mem = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var sheet = wb.Worksheets.Add(PitExcelImporter.SheetName);
            WriteHeaderRow(sheet);
            // Row 6: fully valid
            WriteDataRow(sheet, 6, "EMP-001", "0000001", "0123456789", "Nguyễn Văn A",
                year: 2025, taxable: 180_000_000m, tax: 18_000_000m, monthFrom: 1, monthTo: 12);
            // Row 7: missing tax code → Error
            WriteDataRow(sheet, 7, "EMP-002", "0000002", "", "Trần Thị B",
                year: 2025, taxable: 120_000_000m, tax: 12_000_000m);
            // Row 8: invalid month range (from=10 > to=3) → Error
            WriteDataRow(sheet, 8, "EMP-003", "0000003", "9876543210", "Lê Văn C",
                year: 2025, taxable: 90_000_000m, tax: 9_000_000m, monthFrom: 10, monthTo: 3);
            wb.SaveAs(mem);
        }
        mem.Position = 0;
        return mem;
    }

    private static void WriteHeaderRow(IXLWorksheet sheet)
    {
        var r = sheet.Row(PitExcelImporter.HeaderRowNumber);
        var headers = new[]
        {
            "TaxPayerCode", "ProformaNo", "TaxPayerTaxCode", "TaxPayerName",
            "IncomePaymentYear", "TotalTaxableIncome", "AmountPersonalIncomeTax",
            "IncomePaymentMonthFrom", "IncomePaymentMonthTo"
        };
        for (var i = 0; i < headers.Length; i++)
            r.Cell(i + 1).Value = headers[i];
    }

    private static void WriteDataRow(
        IXLWorksheet sheet, int rowNumber,
        string code, string proforma, string taxCode, string name,
        int year = 2025, decimal taxable = 0m, decimal tax = 0m,
        int? monthFrom = null, int? monthTo = null)
    {
        var r = sheet.Row(rowNumber);
        r.Cell(1).Value = code;
        r.Cell(2).Value = proforma;
        r.Cell(3).Value = taxCode;
        r.Cell(4).Value = name;
        r.Cell(5).Value = year;
        r.Cell(6).Value = (double)taxable;
        r.Cell(7).Value = (double)tax;
        if (monthFrom.HasValue) r.Cell(8).Value = monthFrom.Value;
        if (monthTo.HasValue) r.Cell(9).Value = monthTo.Value;
    }
}
