namespace SPC.BO.PIT;

/// <summary>Lifecycle of a PIT withholding certificate. Stored as text in the database.</summary>
public static class CertificateStatus
{
    public const string Draft = "Draft";
    public const string XmlGenerated = "XmlGenerated";
    public const string Signed = "Signed";
    public const string Submitted = "Submitted";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";

    public static readonly string[] All =
        [Draft, XmlGenerated, Signed, Submitted, Accepted, Rejected];
}
