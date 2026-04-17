namespace SPC.Infrastructure.XmlSigning;

public sealed class XmlSigningOptions
{
    /// <summary>Implementation mode: "Stub" (no-op) or "LocalAgent" (HTTP call).</summary>
    public string Mode { get; set; } = "Stub";

    /// <summary>LocalAgent endpoint, e.g. <c>http://localhost:9999/sign</c>.</summary>
    public string Endpoint { get; set; } = "http://localhost:9999/sign";
}
