using System.Text.Json.Serialization;
using SPC.BO.PIT.Xml;

namespace SPC.Infrastructure.TvanSubmission.Viettel;

/// <summary>
/// Maps a <see cref="TvanSubmissionRequest"/> to the JSON payload Viettel expects at
/// <c>createTaxDeductionCertificate/{supplierTaxCode}</c>. The shape mirrors the
/// integration guide v1.0 exactly (three top-level sections).
/// </summary>
internal static class ViettelPayloadMapper
{
    public static ViettelCreateCertRequest Build(TvanSubmissionRequest request, ViettelOptions cfg)
    {
        var cert = request.Certificate;
        var settings = request.Settings;

        var nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return new ViettelCreateCertRequest(
            GeneralInvoiceInfo: new ViettelGeneralInvoiceInfo(
                TransactionUuid: Guid.NewGuid().ToString(),
                InvoiceType: cfg.InvoiceType,
                TemplateCode: cfg.TemplateCode,
                InvoiceSeries: cfg.InvoiceSeries,
                InvoiceIssuedDate: nowMillis,
                CurrencyCode: cfg.CurrencyCode,
                ExchangeRate: 1,
                AdjustmentType: cert.IsReplacement ? "2" : "1",
                PaymentStatus: true,
                CusGetInvoiceRight: true,
                OriginalInvoiceId: cert.RelatedProformaNo,
                OriginalInvoiceIssueDate: cert.IsReplacement ? nowMillis : (long?)null,
                AdjustmentInvoiceType: cert.IsReplacement ? "2" : null,
                AdditionalReferenceDesc: cert.IsReplacement ? "Chứng từ thay thế" : null,
                AdditionalReferenceDate: cert.IsReplacement ? nowMillis : (long?)null),
            TaxPayerInfo: new ViettelTaxPayerInfo(
                TaxpayerName: cert.TaxPayerName,
                TaxpayerTaxCode: cert.TaxPayerTaxCode,
                TaxpayerAddress: cert.Address,
                TaxpayerNationality: cert.Nationality,
                TaxpayerResidence: ResolveResidence(cert.ResidentType),
                TaxpayerIdNumber: cert.IdentificationNo,
                TaxpayerPhoneNumber: cert.Phone,
                TaxpayerMailAddress: cert.Email,
                TaxpayerNote: cert.Note),
            IncomeInfo: new ViettelIncomeInfo(
                IncomeType: cert.IncomeType ?? "Tiền lương",
                PaymentStartMonth: cert.IncomePaymentMonthFrom ?? 1,
                PaymentEndMonth: cert.IncomePaymentMonthTo ?? 12,
                PaymentYear: cert.IncomePaymentYear,
                CharityAmount: cert.CharityDonations ?? 0m,
                InsuranceAmount: cert.InsurancePremiums ?? 0m,
                TotalTaxableIncome: cert.TotalTaxableIncome,
                TotalTaxCalculationIncome: ComputeTaxCalcIncome(cert),
                AmountOfPersonalIncomeTaxWithheld: cert.AmountPersonalIncomeTax));
    }

    /// <summary>Tổng thu nhập tính thuế = TotalTaxable - Insurance - Charity (non-negative).</summary>
    private static decimal ComputeTaxCalcIncome(PitCertificateXmlInput cert)
    {
        var gross = cert.TotalTaxableIncome
                  - (cert.InsurancePremiums ?? 0m)
                  - (cert.CharityDonations ?? 0m);
        return gross < 0m ? 0m : gross;
    }

    /// <summary>Residence flag per Viettel: 1 = resident (cư trú), 0 = non-resident.</summary>
    private static int ResolveResidence(string? residentType) =>
        string.Equals(residentType, "00081", StringComparison.Ordinal) ? 1 : 0;
}

// --- Wire types: match Viettel's JSON property names exactly (camelCase) ---

internal sealed record ViettelCreateCertRequest(
    [property: JsonPropertyName("generalInvoiceInfo")] ViettelGeneralInvoiceInfo GeneralInvoiceInfo,
    [property: JsonPropertyName("taxPayerInfo")] ViettelTaxPayerInfo TaxPayerInfo,
    [property: JsonPropertyName("incomeInfo")] ViettelIncomeInfo IncomeInfo);

internal sealed record ViettelGeneralInvoiceInfo(
    [property: JsonPropertyName("transactionUuid")] string TransactionUuid,
    [property: JsonPropertyName("invoiceType")] string InvoiceType,
    [property: JsonPropertyName("templateCode")] string TemplateCode,
    [property: JsonPropertyName("invoiceSeries")] string InvoiceSeries,
    [property: JsonPropertyName("invoiceIssuedDate")] long InvoiceIssuedDate,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("exchangeRate")] int ExchangeRate,
    [property: JsonPropertyName("adjustmentType")] string AdjustmentType,
    [property: JsonPropertyName("paymentStatus")] bool PaymentStatus,
    [property: JsonPropertyName("cusGetInvoiceRight")] bool CusGetInvoiceRight,
    [property: JsonPropertyName("originalInvoiceId")] string? OriginalInvoiceId,
    [property: JsonPropertyName("originalInvoiceIssueDate")] long? OriginalInvoiceIssueDate,
    [property: JsonPropertyName("adjustmentInvoiceType")] string? AdjustmentInvoiceType,
    [property: JsonPropertyName("additionalReferenceDesc")] string? AdditionalReferenceDesc,
    [property: JsonPropertyName("additionalReferenceDate")] long? AdditionalReferenceDate);

internal sealed record ViettelTaxPayerInfo(
    [property: JsonPropertyName("taxpayerName")] string TaxpayerName,
    [property: JsonPropertyName("taxpayerTaxCode")] string TaxpayerTaxCode,
    [property: JsonPropertyName("taxpayerAddress")] string? TaxpayerAddress,
    [property: JsonPropertyName("taxpayerNationality")] string? TaxpayerNationality,
    [property: JsonPropertyName("taxpayerResidence")] int TaxpayerResidence,
    [property: JsonPropertyName("taxpayerIdNumber")] string? TaxpayerIdNumber,
    [property: JsonPropertyName("taxpayerPhoneNumber")] string? TaxpayerPhoneNumber,
    [property: JsonPropertyName("taxpayerMailAddress")] string? TaxpayerMailAddress,
    [property: JsonPropertyName("taxpayerNote")] string? TaxpayerNote);

internal sealed record ViettelIncomeInfo(
    [property: JsonPropertyName("incomeType")] string IncomeType,
    [property: JsonPropertyName("paymentStartMonth")] int PaymentStartMonth,
    [property: JsonPropertyName("paymentEndMonth")] int PaymentEndMonth,
    [property: JsonPropertyName("paymentYear")] int PaymentYear,
    [property: JsonPropertyName("charityAmount")] decimal CharityAmount,
    [property: JsonPropertyName("insuranceAmount")] decimal InsuranceAmount,
    [property: JsonPropertyName("totalTaxableIncome")] decimal TotalTaxableIncome,
    [property: JsonPropertyName("totalTaxCalculationIncome")] decimal TotalTaxCalculationIncome,
    [property: JsonPropertyName("amountOfPersonalIncomeTaxWithheld")] decimal AmountOfPersonalIncomeTaxWithheld);
