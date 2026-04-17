namespace SPC.BO.PIT;

public enum ImportSeverity { Error, Warning }

/// <summary>
/// A problem encountered on a specific row/column during Excel import.
/// <see cref="ColumnNumber"/> is 1-based; <see cref="ColumnLetter"/> is the Excel-style letter
/// (A, B, …, AA) so users can jump straight to the offending cell. Both are null when the
/// problem is not tied to a specific column (e.g. a missing header or a whole-sheet issue).
/// </summary>
public sealed record RowDiagnostic(
    int RowNumber,
    int? ColumnNumber,
    string? ColumnLetter,
    string? Field,
    string Message,
    ImportSeverity Severity)
{
    /// <summary>Human-readable "E7" style cell reference, or empty when no column is known.</summary>
    public string CellRef => ColumnLetter is null ? string.Empty : $"{ColumnLetter}{RowNumber}";
}

/// <summary>Plain-data snapshot of one imported row, ready to be committed via CSLA.</summary>
public sealed record PitCertificateImportRow
{
    public int ImportRowNumber { get; init; }
    public string TaxPayerCode { get; init; } = "";
    public string ProformaNo { get; init; } = "";
    public string TaxPayerTaxCode { get; init; } = "";
    public string TaxPayerName { get; init; } = "";
    public string? Nationality { get; init; }
    public string? ResidentType { get; init; }
    public string? IdentificationNo { get; init; }
    public DateTime? IssueDate { get; init; }
    public string? IssuePlace { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public decimal? InsurancePremiums { get; init; }
    public decimal? CharityDonations { get; init; }
    public int? IncomePaymentMonthFrom { get; init; }
    public int? IncomePaymentMonthTo { get; init; }
    public int IncomePaymentYear { get; init; }
    public decimal TotalTaxableIncome { get; init; }
    public decimal AmountPersonalIncomeTax { get; init; }
    public decimal? IncomeStillReceivable { get; init; }
    public string? IncomeType { get; init; }
    public string? Note { get; init; }
    public string? RelatedProformaNo { get; init; }
    public string? RelatedFormNo { get; init; }
}

/// <summary>Outcome of an <see cref="PitExcelImporter"/> parse: rows + per-row diagnostics.</summary>
public sealed record PitImportResult(
    IReadOnlyList<PitCertificateImportRow> Rows,
    IReadOnlyList<RowDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(d => d.Severity == ImportSeverity.Error);
    public int ErrorCount => Diagnostics.Count(d => d.Severity == ImportSeverity.Error);
    public int WarningCount => Diagnostics.Count(d => d.Severity == ImportSeverity.Warning);
}
