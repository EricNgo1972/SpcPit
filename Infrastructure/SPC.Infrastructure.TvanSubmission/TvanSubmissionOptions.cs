namespace SPC.Infrastructure.TvanSubmission;

public sealed class TvanSubmissionOptions
{
    /// <summary>"Stub" (in-process fake) or a real provider name ("Viettel", "VNPT", "MISA", …).</summary>
    public string Mode { get; set; } = "Stub";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}
