namespace Tickflo.Core.Services.Widgets;

using Tickflo.Core.Entities;

/// <summary>
/// The result of a single widget fetch.
/// </summary>
public sealed record WidgetFetchResult(string? Value, WidgetHealth Health, string? ErrorMessage);

/// <summary>
/// Fetches the current status for a widget from its external source.
/// Implementations live in the web host (they use <see cref="System.Net.Http.IHttpClientFactory"/>);
/// the interface lives in Core so the polling service can stay host-agnostic.
/// </summary>
public interface IWidgetSource
{
    public WidgetType Type { get; }

    public Task<WidgetFetchResult> FetchAsync(Widget widget, string? secret, CancellationToken cancellationToken);
}
