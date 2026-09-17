namespace Tickflo.Web.Services;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Widgets;

/// <summary>
/// Configuration for the Wazuh widget, deserialized from <see cref="Widget.ConfigJson"/>.
/// </summary>
public sealed record WazuhWidgetConfig(string? Username, string? Metric);

/// <summary>
/// Reads a metric from the Wazuh server API (port 55000). Authenticates with a
/// username/password to obtain a JWT, then queries either the agent count or the
/// number of critical (level &gt;= 10) alerts.
/// </summary>
public class WazuhWidgetSource(IHttpClientFactory httpClientFactory) : IWidgetSource
{
    private const string CriticalAlertsMetric = "criticalAlerts";

    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    public WidgetType Type => WidgetType.Wazuh;

    public async Task<WidgetFetchResult> FetchAsync(Widget widget, string? secret, CancellationToken cancellationToken)
    {
        try
        {
            var config = ParseConfig(widget.ConfigJson);
            if (string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(secret))
            {
                return new WidgetFetchResult(null, WidgetHealth.Error, "A Wazuh widget requires a username and password.");
            }

            var client = this.httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var token = await AuthenticateAsync(client, widget.Url, config.Username, secret, cancellationToken);
            return await FetchMetricAsync(client, widget.Url, token, config.Metric, cancellationToken);
        }
        catch (Exception ex)
        {
            return new WidgetFetchResult(null, WidgetHealth.Error, ex.Message);
        }
    }

    private static async Task<string> AuthenticateAsync(
        HttpClient client,
        string baseUrl,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/security/user/authenticate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("data").GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The Wazuh authentication response did not include a token.");
    }

    private static async Task<WidgetFetchResult> FetchMetricAsync(
        HttpClient client,
        string baseUrl,
        string token,
        string? metric,
        CancellationToken cancellationToken)
    {
        var endpoint = metric == CriticalAlertsMetric
            ? "/events?limit=1&q=rule.level%3E%3D10"
            : "/agents?limit=1";

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}{endpoint}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var total = document.RootElement.GetProperty("data").GetProperty("total_affected_items").GetInt32();

        if (metric == CriticalAlertsMetric)
        {
            return new WidgetFetchResult(
                $"{total} critical alerts",
                total > 0 ? WidgetHealth.Critical : WidgetHealth.Ok,
                null);
        }

        return new WidgetFetchResult($"{total} agents", WidgetHealth.Ok, null);
    }

    private static WazuhWidgetConfig ParseConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<WazuhWidgetConfig>(configJson)
                ?? new WazuhWidgetConfig(null, null);
        }
        catch
        {
            return new WazuhWidgetConfig(null, null);
        }
    }
}
