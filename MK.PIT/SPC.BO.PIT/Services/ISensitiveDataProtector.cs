namespace SPC.BO.PIT;

/// <summary>
/// Encrypts/decrypts sensitive strings (passwords, API keys) before they cross the BO↔DAL
/// boundary so the DB never contains plaintext secrets. Resolved via CSLA's
/// <c>ApplicationContext.GetRequiredService&lt;ISensitiveDataProtector&gt;()</c> — the
/// concrete implementation lives in the composition root and is typically backed by
/// ASP.NET Core Data Protection.
/// </summary>
public interface ISensitiveDataProtector
{
    /// <summary>Returns an opaque ciphertext. Safe to persist to disk / DB.</summary>
    string Protect(string plaintext);

    /// <summary>Reverses <see cref="Protect"/>. Throws if the ciphertext is tampered with, from a different key ring, or malformed.</summary>
    string Unprotect(string ciphertext);
}
