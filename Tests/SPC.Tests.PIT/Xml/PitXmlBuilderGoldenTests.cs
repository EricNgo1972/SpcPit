using System.Text;
using FluentAssertions;
using SPC.BO.PIT.Xml;
using Xunit;

namespace SPC.Tests.PIT.Xml;

public class PitXmlBuilderGoldenTests
{
    private static readonly DateTime FixedUtc = new(2026, 4, 17, 2, 0, 0, DateTimeKind.Utc);
    private const string FixedMessageId = "VCTY0001ABCDEF0123456789ABCDEF0123456789";

    [Fact]
    public void Builds_envelope_with_expected_TTChung_values()
    {
        var builder = new PitXmlBuilder();
        var result = builder.Build(BuildContext(SimpleCert(), SimpleSettings()));

        var xml = Encoding.UTF8.GetString(result.Xml);
        xml.Should().Contain("<TDiep>");
        xml.Should().Contain("<PBan>2.0.0</PBan>");
        xml.Should().Contain("<MNGui>VCTY0001</MNGui>");
        xml.Should().Contain("<MNNhan>TCT</MNNhan>");
        xml.Should().Contain("<MLTDiep>201</MLTDiep>");
        xml.Should().Contain($"<MTDiep>{FixedMessageId}</MTDiep>");
        xml.Should().Contain("<MST>0123456789</MST>");
        xml.Should().Contain("<SLuong>1</SLuong>");
    }

    [Fact]
    public void Omits_MTDTChieu_content_for_non_replacement_cert()
    {
        var builder = new PitXmlBuilder();
        var result = builder.Build(BuildContext(SimpleCert(), SimpleSettings()));
        var xml = Encoding.UTF8.GetString(result.Xml);
        (xml.Contains("<MTDTChieu></MTDTChieu>") || xml.Contains("<MTDTChieu />"))
            .Should().BeTrue("empty reference for new certs");
    }

    [Fact]
    public void Populates_MTDTChieu_when_replacing_prior_certificate()
    {
        var cert = SimpleCert() with
        {
            RelatedProformaNo = "00001234",
            RelatedFormNo = "0001"
        };

        var builder = new PitXmlBuilder();
        var xml = Encoding.UTF8.GetString(builder.Build(BuildContext(cert, SimpleSettings())).Xml);

        xml.Should().Contain("<MTDTChieu>00001234</MTDTChieu>");
        xml.Should().Contain("<CTuLQuan>");
        xml.Should().Contain("<So>00001234</So>");
    }

    [Fact]
    public void Writes_amounts_with_invariant_culture_and_no_scientific_notation()
    {
        var cert = SimpleCert() with
        {
            Address = null,  // avoid commas in textual fields so the assertion below is about numbers only
            TotalTaxableIncome = 123456789.123456m,
            AmountPersonalIncomeTax = 12345678.9m
        };
        var settings = SimpleSettings() with { OrganizationAddress = null };

        var builder = new PitXmlBuilder();
        var xml = Encoding.UTF8.GetString(builder.Build(BuildContext(cert, settings)).Xml);

        xml.Should().Contain("<TongTNCTinhThue>123456789.123456</TongTNCTinhThue>");
        xml.Should().Contain("<TongTNTTNCN>12345678.9</TongTNTTNCN>");
        xml.Should().NotContain("E+");
        xml.Should().NotContain(",", "amounts never use thousands separators (no commas anywhere once text fields are removed)");
    }

    [Fact]
    public void Emits_Signature_placeholder_when_requested()
    {
        var builder = new PitXmlBuilder();
        var result = builder.Build(BuildContext(SimpleCert(), SimpleSettings(), emitSignature: true));
        var xml = Encoding.UTF8.GetString(result.Xml);
        xml.Should().Contain("http://www.w3.org/2000/09/xmldsig#");
        xml.Should().Contain("Signature");
    }

    [Fact]
    public void Omits_Signature_placeholder_when_not_requested()
    {
        var builder = new PitXmlBuilder();
        var result = builder.Build(BuildContext(SimpleCert(), SimpleSettings(), emitSignature: false));
        var xml = Encoding.UTF8.GetString(result.Xml);
        xml.Should().NotContain("xmldsig");
    }

    [Fact]
    public void Output_is_utf8_without_bom()
    {
        var builder = new PitXmlBuilder();
        var result = builder.Build(BuildContext(SimpleCert(), SimpleSettings()));
        result.Xml[0].Should().Be((byte)'<', "UTF-8 BOM (EF BB BF) is prohibited by QĐ 1306");
    }

    [Fact]
    public void Returns_messageId_unchanged()
    {
        var builder = new PitXmlBuilder();
        var result = builder.Build(BuildContext(SimpleCert(), SimpleSettings()));
        result.MessageId.Should().Be(FixedMessageId);
    }

    // --- Fixtures ---

    private static PitXmlBuildContext BuildContext(
        PitCertificateXmlInput cert,
        PitSettingsXmlInput settings,
        bool emitSignature = true) =>
        new(cert, settings, FixedMessageId, FixedUtc, emitSignature);

    private static PitCertificateXmlInput SimpleCert() =>
        new(
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
            IncomeStillReceivable: null,
            IncomeType: "TL",
            Note: null,
            RelatedProformaNo: null,
            RelatedFormNo: null);

    private static PitSettingsXmlInput SimpleSettings() =>
        new(
            OrganizationTaxCode: "0123456789",
            OrganizationName: "Công ty Cổ phần Thử Nghiệm",
            OrganizationAddress: "456 Nguyễn Huệ, Q.1, TP.HCM",
            OrganizationPhone: "02812345678",
            OrganizationEmail: "contact@example.com",
            SenderCode: "VCTY0001",
            XmlSchemaVersion: "2.0.0",
            XmlMessageTypeCode: "201");
}
