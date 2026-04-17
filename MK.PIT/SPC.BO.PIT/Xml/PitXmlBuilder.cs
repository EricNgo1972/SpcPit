using System.Text;
using System.Xml;

namespace SPC.BO.PIT.Xml;

/// <summary>Input parameters for building a QĐ 1306-compliant PIT withholding certificate XML.</summary>
public sealed record PitXmlBuildContext(
    PitCertificateXmlInput Certificate,
    PitSettingsXmlInput Settings,
    string MessageId,
    DateTime GeneratedAtUtc,
    bool EmitSignaturePlaceholder = true);

/// <summary>Result of a build: the serialized bytes plus the message ID used in TTChung.</summary>
public sealed record PitXmlBuildResult(byte[] Xml, string MessageId);

/// <summary>
/// Builds the QĐ 1306 XML envelope for a single PIT withholding certificate.
/// Element-order sensitive; uses <see cref="XmlWriter"/> for byte-level control so the
/// output can be canonicalized and signed later without surprises.
/// </summary>
public sealed class PitXmlBuilder
{
    private const int MaxMessageSizeBytes = 2_000_000;

    public PitXmlBuildResult Build(PitXmlBuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(ctx.Certificate);
        ArgumentNullException.ThrowIfNull(ctx.Settings);
        if (string.IsNullOrWhiteSpace(ctx.MessageId))
            throw new ArgumentException("MessageId is required.", nameof(ctx));

        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            OmitXmlDeclaration = false,
            NewLineHandling = NewLineHandling.None
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            WriteEnvelope(writer, ctx);
        }

        var xml = stream.ToArray();
        if (xml.Length > MaxMessageSizeBytes)
            throw new InvalidOperationException(
                $"Generated XML is {xml.Length} bytes; QĐ 1306 allows at most {MaxMessageSizeBytes}.");

        return new PitXmlBuildResult(xml, ctx.MessageId);
    }

    private static void WriteEnvelope(XmlWriter w, PitXmlBuildContext ctx)
    {
        var cert = ctx.Certificate;
        var s = ctx.Settings;

        w.WriteStartDocument();
        w.WriteStartElement(PitXmlSchema.TDiep);

        // TTChung
        w.WriteStartElement(PitXmlSchema.TTChung);
        w.WriteElementString(PitXmlSchema.PBan, s.XmlSchemaVersion);
        w.WriteElementString(PitXmlSchema.MNGui, s.SenderCode ?? string.Empty);
        w.WriteElementString(PitXmlSchema.MNNhan, PitXmlSchema.TaxAuthorityReceiver);
        w.WriteElementString(PitXmlSchema.MLTDiep, s.XmlMessageTypeCode);
        w.WriteElementString(PitXmlSchema.MTDiep, ctx.MessageId);
        w.WriteElementString(PitXmlSchema.MTDTChieu, cert.IsReplacement ? (cert.RelatedProformaNo ?? string.Empty) : string.Empty);
        w.WriteElementString(PitXmlSchema.MST, s.OrganizationTaxCode);
        w.WriteElementString(PitXmlSchema.SLuong, "1");
        w.WriteEndElement(); // TTChung

        // DLieu
        w.WriteStartElement(PitXmlSchema.DLieu);
        w.WriteStartElement(PitXmlSchema.CTDTKhauTru);

        WriteDLCT(w, cert, s);

        if (ctx.EmitSignaturePlaceholder)
            w.WriteElementString(PitXmlSchema.Signature, PitXmlSchema.SignatureNs, string.Empty);

        w.WriteEndElement(); // CTDTKhauTru
        w.WriteEndElement(); // DLieu

        w.WriteEndElement(); // TDiep
        w.WriteEndDocument();
    }

    private static void WriteDLCT(XmlWriter w, PitCertificateXmlInput cert, PitSettingsXmlInput s)
    {
        w.WriteStartElement(PitXmlSchema.DLCT);

        // Certificate identification
        w.WriteStartElement(PitXmlSchema.TTChungCTu);
        w.WriteElementString(PitXmlSchema.CertNumber, cert.ProformaNo);
        if (!string.IsNullOrWhiteSpace(cert.TaxPayerCode))
            w.WriteElementString(PitXmlSchema.CertFormNo, cert.TaxPayerCode);
        if (cert.IsReplacement)
        {
            w.WriteStartElement(PitXmlSchema.RelatedCert);
            if (!string.IsNullOrWhiteSpace(cert.RelatedFormNo))
                w.WriteElementString(PitXmlSchema.RelatedFormNo, cert.RelatedFormNo);
            w.WriteElementString(PitXmlSchema.RelatedNumber, cert.RelatedProformaNo ?? string.Empty);
            w.WriteEndElement();
        }
        w.WriteEndElement(); // TTChungCTu

        // Issuing organization (employer)
        w.WriteStartElement(PitXmlSchema.TTTo);
        w.WriteElementString(PitXmlSchema.TMST, s.OrganizationTaxCode);
        w.WriteElementString(PitXmlSchema.OName, s.OrganizationName);
        if (!string.IsNullOrWhiteSpace(s.OrganizationAddress))
            w.WriteElementString(PitXmlSchema.OAddress, s.OrganizationAddress);
        if (!string.IsNullOrWhiteSpace(s.OrganizationPhone))
            w.WriteElementString(PitXmlSchema.OPhone, s.OrganizationPhone);
        if (!string.IsNullOrWhiteSpace(s.OrganizationEmail))
            w.WriteElementString(PitXmlSchema.OEmail, s.OrganizationEmail);
        w.WriteEndElement(); // TTTo

        // Taxpayer (staff member)
        w.WriteStartElement(PitXmlSchema.TTNNT);
        w.WriteElementString(PitXmlSchema.TName, cert.TaxPayerName);
        w.WriteElementString(PitXmlSchema.TMST, cert.TaxPayerTaxCode);
        if (!string.IsNullOrWhiteSpace(cert.Nationality))
            w.WriteElementString(PitXmlSchema.TNat, cert.Nationality!);
        if (!string.IsNullOrWhiteSpace(cert.ResidentType))
            w.WriteElementString(PitXmlSchema.TResType, cert.ResidentType!);
        if (!string.IsNullOrWhiteSpace(cert.IdentificationNo))
            w.WriteElementString(PitXmlSchema.TIdNo, cert.IdentificationNo!);
        if (cert.IssueDate.HasValue)
            w.WriteElementString(PitXmlSchema.TIssueDate, VnDateTime.FormatDate(cert.IssueDate.Value));
        if (!string.IsNullOrWhiteSpace(cert.IssuePlace))
            w.WriteElementString(PitXmlSchema.TIssuePlace, cert.IssuePlace!);
        if (!string.IsNullOrWhiteSpace(cert.Phone))
            w.WriteElementString(PitXmlSchema.TPhone, cert.Phone!);
        if (!string.IsNullOrWhiteSpace(cert.Email))
            w.WriteElementString(PitXmlSchema.TEmail, cert.Email!);
        if (!string.IsNullOrWhiteSpace(cert.Address))
            w.WriteElementString(PitXmlSchema.TAddress, cert.Address!);
        w.WriteEndElement(); // TTNNT

        // Withholding figures
        w.WriteStartElement(PitXmlSchema.TTKhauTru);
        if (cert.IncomePaymentMonthFrom.HasValue)
            w.WriteElementString(PitXmlSchema.TPeriodFrom, cert.IncomePaymentMonthFrom.Value.ToString("00"));
        if (cert.IncomePaymentMonthTo.HasValue)
            w.WriteElementString(PitXmlSchema.TPeriodTo, cert.IncomePaymentMonthTo.Value.ToString("00"));
        w.WriteElementString(PitXmlSchema.TPeriodYear, cert.IncomePaymentYear.ToString("0000"));
        if (!string.IsNullOrWhiteSpace(cert.IncomeType))
            w.WriteElementString(PitXmlSchema.TIncomeType, cert.IncomeType!);
        w.WriteElementString(PitXmlSchema.TTaxableIncome, VnDecimal.Format(cert.TotalTaxableIncome));
        w.WriteElementString(PitXmlSchema.TTaxWithheld, VnDecimal.Format(cert.AmountPersonalIncomeTax));
        if (cert.InsurancePremiums.HasValue)
            w.WriteElementString(PitXmlSchema.TInsurance, VnDecimal.Format(cert.InsurancePremiums.Value));
        if (cert.CharityDonations.HasValue)
            w.WriteElementString(PitXmlSchema.TCharity, VnDecimal.Format(cert.CharityDonations.Value));
        if (cert.IncomeStillReceivable.HasValue)
            w.WriteElementString(PitXmlSchema.TStillReceivable, VnDecimal.Format(cert.IncomeStillReceivable.Value));
        if (!string.IsNullOrWhiteSpace(cert.Note))
            w.WriteElementString(PitXmlSchema.TNote, cert.Note!);
        w.WriteEndElement(); // TTKhauTru

        w.WriteEndElement(); // DLCT
    }
}
