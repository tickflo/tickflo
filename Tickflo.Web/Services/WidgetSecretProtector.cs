namespace Tickflo.Web.Services;

using Microsoft.AspNetCore.DataProtection;
using Tickflo.Core.Services.Widgets;

/// <summary>
/// Reversibly encrypts widget secrets using ASP.NET Core Data Protection.
/// The key ring is persisted to disk so secrets survive application restarts.
/// </summary>
public class WidgetSecretProtector(IDataProtectionProvider provider) : IWidgetSecretProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("Tickflo.Widget.Secret.v1");

    public string Protect(string plaintext) => this.protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => this.protector.Unprotect(ciphertext);
}
