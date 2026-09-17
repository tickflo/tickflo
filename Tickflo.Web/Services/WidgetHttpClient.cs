namespace Tickflo.Web.Services;
/// <summary>
/// Creates the HTTP client a widget source uses. Self-hosted widgets monitoring
/// internal infrastructure (Wazuh, LAN services) commonly talk to endpooints that
/// rely on self-signed certificates, so a per-widget <c>skipTlsVerify</c> opt-in
/// relaxes certificate validation for exactly those widgets. The public/multi-tenant
/// path (certificate validation enforced) is the default and only used when the widget
/// does NOT opt in.
/// </summary>
public static class WidgetHttpClient
{
    public static HttpClient Create(IHttpClientFactory factory, bool skipTlsVerify)
    {
        if (!skipTlsVerify)
        {
            var client = factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            return client;
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        var insecureClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        return insecureClient;
    }
}
