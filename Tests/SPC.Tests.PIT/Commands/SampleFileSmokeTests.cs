using FluentAssertions;
using SPC.BO.PIT;
using Xunit;

namespace SPC.Tests.PIT.Commands;

/// <summary>Smoke tests that parse the committed sample xlsx files under /samples.</summary>
public class SampleFileSmokeTests
{
    private static readonly string SamplesDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    [Fact]
    public void Valid_sample_parses_with_no_errors_and_expected_row_count()
    {
        var path = Path.Combine(SamplesDir, "epit-sample-valid.xlsx");
        File.Exists(path).Should().BeTrue($"expected sample at {path}");

        using var stream = File.OpenRead(path);
        var result = new PitExcelImporter().Parse(stream);

        result.HasErrors.Should().BeFalse(because: "valid sample should have zero errors");
        result.Rows.Should().HaveCount(5);
        result.Rows.Select(r => r.TaxPayerName).Should().Contain("Nguyễn Văn An");
    }

    [Fact]
    public void Error_sample_reports_each_deliberate_problem()
    {
        var path = Path.Combine(SamplesDir, "epit-sample-with-errors.xlsx");
        File.Exists(path).Should().BeTrue($"expected sample at {path}");

        using var stream = File.OpenRead(path);
        var result = new PitExcelImporter().Parse(stream);

        result.HasErrors.Should().BeTrue();

        // Missing tax code (row 7, column C, field TaxPayerTaxCode)
        result.Diagnostics.Should().Contain(d =>
            d.RowNumber == 7 && d.Field == "TaxPayerTaxCode" && d.CellRef == "C7" && d.Severity == ImportSeverity.Error);

        // Wrong-length tax code (row 8, column C)
        result.Diagnostics.Should().Contain(d =>
            d.RowNumber == 8 && d.Field == "TaxPayerTaxCode" && d.CellRef == "C8" && d.Message.Contains("10 or 13"));

        // MonthFrom > MonthTo (row 9, column O)
        result.Diagnostics.Should().Contain(d =>
            d.RowNumber == 9 && d.Field == "IncomePaymentMonthFrom" && d.CellRef == "O9");

        // Missing ProformaNo (row 10, column B) and TaxPayerName (row 10, column D)
        result.Diagnostics.Should().Contain(d =>
            d.RowNumber == 10 && d.Field == "ProformaNo" && d.CellRef == "B10");
        result.Diagnostics.Should().Contain(d =>
            d.RowNumber == 10 && d.Field == "TaxPayerName" && d.CellRef == "D10");
    }
}
