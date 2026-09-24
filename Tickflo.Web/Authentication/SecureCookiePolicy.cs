namespace Tickflo.Web.Authentication;

using Tickflo.Core.Config;

/// <summary>
/// Centralized Secure-flag policy for app-managed cookies.
/// The app runs behind a TLS-terminating reverse proxy (nginx-proxy-manager), so
/// the backend always receives plain HTTP and Request.IsHttps is false even for
/// HTTPS traffic. The session bearer-token cookie must therefore be marked Secure
/// in any non-development environment so it is never transmitted over plaintext
/// (which would let an on-path attacker capture the session).
/// </summary>
public static class SecureCookiePolicy
{
    public static bool IsSecure(TickfloConfig config) =>
        !string.Equals(config.AppEnv, "Development", StringComparison.OrdinalIgnoreCase);
}
