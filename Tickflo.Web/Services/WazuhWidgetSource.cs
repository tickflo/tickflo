namespace Tickflo.Web.Services;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Widgets;

/// <summary>
/// Configuration for the Wazuh widget, deserialized from <see cref="Widget.ConfigJson"/>.
/// The indexer and the manager API use separate credentials, so each metric names its
/// own username; the widget's <see cref="Widget.ApiKeyCiphertext"/> holds the password
/// for the selected metric's backend.
/// </summary>
public sealed record WazuhWidgetConfig(string? Metric, string? IndexerUrl, string? IndexerUsername, string? ApiUsername);

/// <summary>
/// Reads a metric from a Wazuh deployment. <c>agents</c> queries the manager API
/// (port 55000, JWT auth) for the agent count; <c>criticalAlerts</c> queries the
/// Wazuh indexer (port 9200, basic auth) for the number of level &gt;= 10 alerts.
/// </summary>
public class WazuhWidgetSource(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache) : IWidgetSource
{
    private const string CriticalAlertsMetric = "criticalAlerts";
    private const string AgentsMetric = "agents";
    private const string DefaultIndexerUsername = "admin";
    private const string DefaultApiUsername = "wazuh-wui";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(14); // Wazuh JWTs last 900s; refresh early

    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IMemoryCache memoryCache = memoryCache;

    public WidgetType Type => WidgetType.Wazuh;

    public async Task<WidgetFetchResult> FetchAsync(Widget widget, string? secret, CancellationToken cancellationToken)
    {
        try
        {
            var config = ParseConfig(widget.ConfigJson);
            if (string.IsNullOrWhiteSpace(secret))
            {
                return new WidgetFetchResult(null, WidgetHealth.Error, "A Wazuh widget requires a password.");
            }

            var client = this.httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var metric = string.IsNullOrWhiteSpace(config.Metric) ? AgentsMetric : config.Metric;
            if (metric.Equals(CriticalAlertsMetric, StringComparison.OrdinalIgnoreCase))
            {
                return await FetchCriticalAlertsAsync(client, config, secret, cancellationToken);
            }

            return await this.FetchAgentCountAsync(client, widget, config, secret, cancellationToken);
        }
        catch (Exception ex)
        {
            return new WidgetFetchResult(null, WidgetHealth.Error, ex.Message);
        }
    }

    private async Task<WidgetFetchResult> FetchAgentCountAsync(
        HttpClient client,
        Widget widget,
        WazuhWidgetConfig config,
        string secret,
        CancellationToken cancellationToken)
    {
        var username = string.IsNullOrWhiteSpace(config.ApiUsername) ? DefaultApiUsername : config.ApiUsername;
        var token = await this.GetManagerTokenAsync(client, widget.Url, username, secret, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{widget.Url.TrimEnd('/')}/agents?limit=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var total = document.RootElement.GetProperty("data").GetProperty("total_affected_items").GetInt32();

        return new WidgetFetchResult($"{total} agents", WidgetHealth.Ok, null);
    }

    private static async Task<WidgetFetchResult> FetchCriticalAlertsAsync(
        HttpClient client,
        WazuhWidgetConfig config,
        string secret,
        CancellationToken cancellationToken)
    {
        var indexerUrl = config.IndexerUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(indexerUrl))
        {
            return new WidgetFetchResult(null, WidgetHealth.Error, "The criticalAlerts metric requires an indexerUrl.");
        }

        var username = string.IsNullOrWhiteSpace(config.IndexerUsername) ? DefaultIndexerUsername : config.IndexerUsername;
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{secret}"));
        var query = JsonSerializer.Serialize(new { query = new { range = new { rule = new { level = new { gte = 10 } } } } });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{indexerUrl}/wazuh-alerts-*/_count");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(query, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var count = document.RootElement.GetProperty("count").GetInt32();

        return new WidgetFetchResult(
            $"{count} critical alerts",
            count > 0 ? WidgetHealth.Critical : WidgetHealth.Ok,
            null);
    }

    private async Task<string> GetManagerTokenAsync(
        HttpClient client,
        string baseUrl,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"wazuh-token:{baseUrl}:{username}";
        if (this.memoryCache.TryGetValue<string>(cacheKey, out var cachedToken) && cachedToken is not null)
        {
            return cachedToken;
        }

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/security/user/authenticate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var token = document.RootElement.GetProperty("data").GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The Wazuh authentication response did not include a token.");

        this.memoryCache.Set(cacheKey, token, TokenLifetime);
        return token;
    }

    private static WazuhWidgetConfig ParseConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<WazuhWidgetConfig>(configJson)
                ?? new WazuhWidgetConfig(null, null, null, null);
        }
        catch
        {
            return new WazuhWidgetConfig(null, null, null, null);
        }
    }
}
