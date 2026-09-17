namespace Tickflo.CoreTest.Services.Widgets;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Tickflo.Core.Config;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Widgets;
using Tickflo.Web.Services;
using Xunit;

public class WazuhWidgetSourceTests
{
    [Fact]
    public async Task FetchAsync_WhenAgentsMetric_ShouldReturnAgentCount()
    {
        var handler = new RoutingHandler(new Dictionary<string, object>
        {
            ["/security/user/authenticate"] = new { data = new { token = "test-token" } },
            ["/agents"] = new { data = new { total_affected_items = 14 } },
        });
        var source = CreateSource(handler);
        var widget = CreateWidget("http://8.8.8.8/", JsonSerializer.Serialize(new { metric = "agents" }));

        var result = await source.FetchAsync(widget, "secret", CancellationToken.None);

        Assert.Equal(WidgetHealth.Ok, result.Health);
        Assert.Equal("14 agents", result.Value);
    }

    [Fact]
    public async Task FetchAsync_WhenCriticalAlertsMetric_ShouldReturnCount()
    {
        var handler = new RoutingHandler(new Dictionary<string, object>
        {
            ["/_count"] = new { count = 3 },
        });
        var source = CreateSource(handler);
        var widget = CreateWidget(
            "http://8.8.8.8/",
            JsonSerializer.Serialize(new { metric = "criticalAlerts", indexerUrl = "http://8.8.8.8:9200" }));

        var result = await source.FetchAsync(widget, "secret", CancellationToken.None);

        Assert.Equal(WidgetHealth.Critical, result.Health);
        Assert.Equal("3 critical alerts", result.Value);
    }

    private static WazuhWidgetSource CreateSource(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(client);
        return new WazuhWidgetSource(factory.Object, new MemoryCache(new MemoryCacheOptions()), new TickfloConfig());
    }

    private static Widget CreateWidget(string url, string configJson) =>
        new() { Url = url, ConfigJson = configJson, Type = WidgetType.Wazuh };

    private sealed class RoutingHandler(Dictionary<string, object> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            foreach (var (fragment, value) in responses)
            {
                if (request.RequestUri!.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    var body = value is string stringValue ? stringValue : JsonSerializer.Serialize(value);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json"),
                    });
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
