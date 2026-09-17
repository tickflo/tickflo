namespace Tickflo.CoreTest.Services.Widgets;

using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Tickflo.Core.Config;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Widgets;
using Tickflo.Web.Services;
using Xunit;

public class GenericHttpWidgetSourceTests
{
    [Fact]
    public async Task FetchAsync_WhenValueMatchesExpect_ShouldReturnOk()
    {
        var source = CreateSource(JsonSerializer.Serialize(new { data = new { count = 0 } }));
        var widget = CreateWidget("http://8.8.8.8/", JsonSerializer.Serialize(new { jsonPath = "data.count", expect = "0" }));

        var result = await source.FetchAsync(widget, null, CancellationToken.None);

        Assert.Equal(WidgetHealth.Ok, result.Health);
        Assert.Equal("0", result.Value);
    }

    [Fact]
    public async Task FetchAsync_WhenValueDiffersFromExpect_ShouldReturnCritical()
    {
        var source = CreateSource(JsonSerializer.Serialize(new { data = new { count = 5 } }));
        var widget = CreateWidget("http://8.8.8.8/", JsonSerializer.Serialize(new { jsonPath = "data.count", expect = "0" }));

        var result = await source.FetchAsync(widget, null, CancellationToken.None);

        Assert.Equal(WidgetHealth.Critical, result.Health);
        Assert.Equal("5", result.Value);
    }

    [Fact]
    public async Task FetchAsync_WhenPrivateUrl_ShouldReturnError()
    {
        var source = CreateSource("{}");
        var widget = CreateWidget("http://192.168.1.1/", "{}");

        var result = await source.FetchAsync(widget, null, CancellationToken.None);

        Assert.Equal(WidgetHealth.Error, result.Health);
    }

    private static GenericHttpWidgetSource CreateSource(string responseBody)
    {
        var handler = new FakeHttpMessageHandler(responseBody);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(client);
        return new GenericHttpWidgetSource(factory.Object, new TickfloConfig());
    }

    private static Widget CreateWidget(string url, string configJson) =>
        new() { Url = url, ConfigJson = configJson, Type = WidgetType.HttpJson };

    private sealed class FakeHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            });
    }
}
