namespace Tickflo.Web.Services;

using System.Net;
using System.Net.Sockets;
using Tickflo.Core.Exceptions;

/// <summary>
/// Guards against server-side request forgery (SSRF) by rejecting widget URLs that
/// resolve to loopback, private, link-local, multicast, or reserved address ranges.
/// When <c>allowPrivateTargets</c> is true (self-hosted), only the http/https scheme
/// check is enforced so operators can monitor their own LAN infrastructure.
/// </summary>
public static class WidgetUrlSafety
{
    private static readonly string[] BlockedHostnameSuffixes = [".localhost", ".local", ".internal", ".home", ".lan"];

    /// <summary>
    /// Throws <see cref="BadRequestException"/> when <paramref name="url"/> is not an
    /// http/https URL (and, unless <paramref name="allowPrivateTargets"/> is true,
    /// resolves to a private/reserved address).
    /// </summary>
    public static async Task EnsureSafeUrlAsync(string url, bool allowPrivateTargets, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new BadRequestException("The widget URL must be an absolute http or https URL.");
        }

        if (allowPrivateTargets)
        {
            return;
        }

        var host = uri.Host;

        if (IPAddress.TryParse(host, out var literalIp))
        {
            EnsureSafeIp(literalIp);
            return;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || BlockedHostnameSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("The widget URL must not target local or internal hostnames.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (SocketException)
        {
            throw new BadRequestException("The widget URL host could not be resolved.");
        }

        foreach (var address in addresses)
        {
            EnsureSafeIp(address);
        }
    }

    private static void EnsureSafeIp(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || IsPrivateOrReserved(address))
        {
            throw new BadRequestException("The widget URL must not target loopback, private, link-local, or reserved addresses.");
        }
    }

    private static bool IsPrivateOrReserved(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            if (bytes[0] is 0 or 10 or 127)
            {
                return true;
            }

            if (bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
            {
                return true; // CGNAT
            }

            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true; // link-local / cloud metadata
            }

            if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            {
                return true; // private
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true; // private
            }

            if (bytes[0] >= 224)
            {
                return true; // multicast + reserved
            }

            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            if (bytes[0] is 0xFC or 0xFD)
            {
                return true; // fc00::/7 unique-local
            }

            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
            {
                return true; // fe80::/10 link-local
            }

            if (bytes[0] == 0xFF)
            {
                return true; // multicast
            }

            return false;
        }

        return false;
    }
}
