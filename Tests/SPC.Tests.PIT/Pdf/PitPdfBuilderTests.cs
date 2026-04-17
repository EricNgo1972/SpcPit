using FluentAssertions;
using QuestPDF.Infrastructure;
using SPC.BO.PIT.Pdf;
using SPC.BO.PIT.Xml;
using Xunit;

namespace SPC.Tests.PIT.Pdf;

public class PitPdfBuilderTests
{
    static PitPdfBuilderTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public void Builds_non_empty_pdf_that_looks_like_a_valid_pdf_file()
    {
        var builder = new PitPdfBuilder();
        var bytes = builder.Build(BuildContext());

        bytes.Should().NotBeEmpty();
        // PDF files start with "%PDF-"
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
        bytes.Length.Should().BeGreaterThan(1000, "a one-page certificate should be at least a kilobyte");
    }

    private static PitPdfBuildContext BuildContext() => new(
        Certificate: new PitCertificateXmlInput(
            TaxPayerCode: "EMP-001",
            ProformaNo: "000001",
            TaxPayerTaxCode: "9876543210",
            TaxPayerName: "Nguyễn Văn A",
            Nationality: "Việt Nam",
            ResidentType: "00081",
            IdentificationNo: "012345678901",
            IssueDate: new DateTime(2020, 5, 15),
            IssuePlace: "CA TP.HCM",
            Phone: "0901234567",
            Email: "a@example.com",
            Address: "123 Lê Lợi, Q.1, TP.HCM",
            InsurancePremiums: 1_500_000m,
            CharityDonations: 500_000m,
            IncomePaymentMonthFrom: 1,
            IncomePaymentMonthTo: 12,
            IncomePaymentYear: 2025,
            TotalTaxableIncome: 180_000_000m,
            AmountPersonalIncomeTax: 18_000_000m,
            IncomeStillReceivable: 162_000_000m,
            IncomeType: "TL",
            Note: null,
            RelatedProformaNo: null,
            RelatedFormNo: null),
        Settings: new PitSettingsXmlInput(
            OrganizationTaxCode: "0123456789",
            OrganizationName: "Công ty Cổ phần Thử Nghiệm",
            OrganizationAddress: "456 Nguyễn Huệ, Q.1, TP.HCM",
            OrganizationPhone: "02812345678",
            OrganizationEmail: "contact@example.com",
            SenderCode: "VCTY0001",
            XmlSchemaVersion: "2.0.0",
            XmlMessageTypeCode: "201"),
        CqtCode: "STUB-ABCDEF012345",
        FormNumber: "1",
        Series: "1/2025/T",
        IssuedOn: new DateTime(2025, 12, 31));
}
