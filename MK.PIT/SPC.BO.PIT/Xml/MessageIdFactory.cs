namespace SPC.BO.PIT.Xml;

/// <summary>
/// Generates <c>MTDiep</c> values per QĐ 1306 §II: a 32-character uppercase hex UUID (v4, no hyphens),
/// optionally prefixed by the sender code (<c>MNGui</c>) so the receiver can attribute the message.
/// </summary>
public sealed class MessageIdFactory
{
    /// <summary>Generate a new message ID. Pass the sender code as prefix when one is configured.</summary>
    public string New(string? senderCode = null)
    {
        var uuid = Guid.NewGuid().ToString("N").ToUpperInvariant();
        return string.IsNullOrWhiteSpace(senderCode) ? uuid : senderCode + uuid;
    }
}
