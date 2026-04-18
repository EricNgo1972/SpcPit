using Microsoft.AspNetCore.DataProtection;
using SPC.BO.PIT;

namespace Main.Services;

/// <summary>
/// Concrete <see cref="ISensitiveDataProtector"/> backed by ASP.NET Core Data Protection.
/// Key material persists to <c>{ContentRoot}/keys</c> (configured in Program.cs) so
/// encrypted values survive app restarts and can be decrypted by the same app on the
/// same host. Losing the keys folder makes all ciphertexts unrecoverable — back it up
/// together with <c>spc-pit.db</c>.
/// </summary>
public sealed class DataProtectionSensitiveDataProtector : ISensitiveDataProtector
{
    // One purpose string per "class of secret" so rotating or repurposing one doesn't
    // affect others. All sensitive strings in the PIT module share this purpose today.
    private const string Purpose = "SPC.BO.PIT.SensitiveData.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSensitiveDataProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
