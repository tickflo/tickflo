namespace Tickflo.Web.Services;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Widgets;

/// <summary>
/// Configuration for the generic HTTP/JSON widget, deserialized from
/// <see cref="Widget.ConfigJson"/>.
/// </summary>
public sealed record GenericHttpWidgetConfig(string? JsonPath, string? Expect);

/// <summary>
/// Reads a value from any public JSON HTTP endpoint. The value is located via a dot
/// path (with optional array indices) and, when <c>Expect</c> is set, the widget
/// reports <see cref="WidgetHealth.Critical"/> if the value differs from it.
/// URLs are validated against SSRF and responses are size-bounded.
/// </summary>
public class GenericHttpWidgetSource(IHttpClientFactory httpClientFactory) : IWidgetSource
{
    private const int MaxResponseBytes = 1_048_576; // 1 MB

    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    public WidgetType Type => WidgetType.HttpJson;

    public async Task<WidgetFetchResult> FetchAsync(Widget widget, string? secret, CancellationToken cancellationToken)
    {
        try
        {
            await WidgetUrlSafety.EnsureSafeUrlAsync(widget.Url, cancellationToken);

            var config = ParseConfig(widget.ConfigJson);
            var client = this.httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using var request = new HttpRequestMessage(HttpMethod.Get, widget.Url);
            if (!string.IsNullOrWhiteSpace(secret))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await ReadBoundedAsync(response, cancellationToken);
            var value = ExtractValue(body, config.JsonPath);

            var health = string.IsNullOrWhiteSpace(config.Expect) || string.Equals(value, config.Expect, StringComparison.Ordinal)
                ? WidgetHealth.Ok
                : WidgetHealth.Critical;

            return new WidgetFetchResult(value, health, null);
        }
        catch (Exception ex)
        {
            return new WidgetFetchResult(null, WidgetHealth.Error, ex.Message);
        }
    }

    private static async Task<string> ReadBoundedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var buffer = new char[8192];
        var builder = new StringBuilder();
        var total = 0;

        while (total < MaxResponseBytes)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                return builder.ToString();
            }

            builder.Append(buffer, 0, read);
            total += read;
        }

        throw new InvalidOperationException("The widget response exceeded the 1 MB size limit.");
    }

    private static GenericHttpWidgetConfig ParseConfig(string configJson)
    {
        try
        {
            return JsonSerializer.Deserialize<GenericHttpWidgetConfig>(configJson)
                ?? new GenericHttpWidgetConfig(null, null);
        }
        catch
        {
            return new GenericHttpWidgetConfig(null, null);
        }
    }

    private static string? ExtractValue(string body, string? jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            return body;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var node = document.RootElement;
            foreach (var segment in jsonPath.Split('.'))
            {
                var (key, index) = ParseSegment(segment);
                if (!node.TryGetProperty(key, out node))
                {
                    return null;
                }

                if (index.HasValue)
                {
                    node = node.EnumerateArray().ElementAtOrDefault(index.Value);
                }
            }

            return NodeToString(node);
        }
        catch
        {
            return null;
        }
    }

    private static (string Key, int? Index) ParseSegment(string segment)
    {
        var openBracket = segment.IndexOf('[');
        if (openBracket < 0)
        {
            return (segment, null);
        }

        var key = segment[..openBracket];
        var closeBracket = segment.IndexOf(']', openBracket);
        if (closeBracket < 0 || !int.TryParse(segment.AsSpan(openBracket + 1, closeBracket - openBracket - 1), out var index))
        {
            return (segment, null);
        }

        return (key, index);
    }

    private static string? NodeToString(JsonElement node) => node.ValueKind switch
    {
        JsonValueKind.String => node.GetString(),
        JsonValueKind.Number => node.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => null,
        JsonValueKind.Undefined => throw new NotImplementedException(),
        JsonValueKind.Object => throw new NotImplementedException(),
        JsonValueKind.Array => throw new NotImplementedException(),
        _ => node.GetRawText(),
    };
}
