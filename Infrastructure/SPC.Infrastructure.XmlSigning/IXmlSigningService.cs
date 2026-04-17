namespace SPC.Infrastructure.XmlSigning;

/// <summary>
/// Abstraction for XML digital signing. Signs an unsigned QĐ 1306 envelope and returns
/// the signed bytes. Implementations delegate to an external LocalAgent, a stub, or any
/// other signing provider that understands W3C XML Digital Signature.
/// </summary>
public interface IXmlSigningService
{
    Task<byte[]> SignAsync(byte[] unsignedXml, CancellationToken cancellationToken = default);
}
