namespace SPC.BO.PIT.Xml;

/// <summary>
/// Plain-data snapshot of a <see cref="PitCertificate"/> passed to the XML builder.
/// Decoupling the builder from CSLA makes it a pure function that's trivial to unit-test.
/// </summary>
public sealed record PitCertificateXmlInput(
    string TaxPayerCode,
    string ProformaNo,
    string TaxPayerTaxCode,
    string TaxPayerName,
    string? Nationality,
    string? ResidentType,
    string? IdentificationNo,
    DateTime? IssueDate,
    string? IssuePlace,
    string? Phone,
    string? Email,
    string? Address,
    decimal? InsurancePremiums,
    decimal? CharityDonations,
    int? IncomePaymentMonthFrom,
    int? IncomePaymentMonthTo,
    int IncomePaymentYear,
    decimal TotalTaxableIncome,
    decimal AmountPersonalIncomeTax,
    decimal? IncomeStillReceivable,
    string? IncomeType,
    string? Note,
    string? RelatedProformaNo,
    string? RelatedFormNo)
{
    public bool IsReplacement => !string.IsNullOrEmpty(RelatedProformaNo);

    public static PitCertificateXmlInput From(PitCertificate cert) =>
        new(
            TaxPayerCode: cert.TaxPayerCode,
            ProformaNo: cert.ProformaNo,
            TaxPayerTaxCode: cert.TaxPayerTaxCode,
            TaxPayerName: cert.TaxPayerName,
            Nationality: cert.Nationality,
            ResidentType: cert.ResidentType,
            IdentificationNo: cert.IdentificationNo,
            IssueDate: cert.IssueDate,
            IssuePlace: cert.IssuePlace,
            Phone: cert.Phone,
            Email: cert.Email,
            Address: cert.Address,
            InsurancePremiums: cert.InsurancePremiums,
            CharityDonations: cert.CharityDonations,
            IncomePaymentMonthFrom: cert.IncomePaymentMonthFrom,
            IncomePaymentMonthTo: cert.IncomePaymentMonthTo,
            IncomePaymentYear: cert.IncomePaymentYear,
            TotalTaxableIncome: cert.TotalTaxableIncome,
            AmountPersonalIncomeTax: cert.AmountPersonalIncomeTax,
            IncomeStillReceivable: cert.IncomeStillReceivable,
            IncomeType: cert.IncomeType,
            Note: cert.Note,
            RelatedProformaNo: cert.RelatedProformaNo,
            RelatedFormNo: cert.RelatedFormNo);
}

/// <summary>Plain-data snapshot of <see cref="PitSettings"/> used by the XML builder.</summary>
public sealed record PitSettingsXmlInput(
    string OrganizationTaxCode,
    string OrganizationName,
    string? OrganizationAddress,
    string? OrganizationPhone,
    string? OrganizationEmail,
    string SenderCode,
    string XmlSchemaVersion,
    string XmlMessageTypeCode)
{
    public static PitSettingsXmlInput From(PitSettings settings) =>
        new(
            OrganizationTaxCode: settings.OrganizationTaxCode,
            OrganizationName: settings.OrganizationName,
            OrganizationAddress: settings.OrganizationAddress,
            OrganizationPhone: settings.OrganizationPhone,
            OrganizationEmail: settings.OrganizationEmail,
            SenderCode: settings.SenderCode,
            XmlSchemaVersion: settings.XmlSchemaVersion,
            XmlMessageTypeCode: settings.XmlMessageTypeCode);
}
