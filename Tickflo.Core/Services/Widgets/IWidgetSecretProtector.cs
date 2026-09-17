namespace Tickflo.Core.Services.Widgets;

/// <summary>
/// Reversibly encrypts/decrypts widget secrets (API keys, tokens, passwords).
/// The implementation is host-specific (ASP.NET Core Data Protection); Core only
/// depends on this contract.
/// </summary>
public interface IWidgetSecretProtector
{
    public string Protect(string plaintext);

    public string Unprotect(string ciphertext);
}
