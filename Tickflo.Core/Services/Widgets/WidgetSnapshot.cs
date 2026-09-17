namespace Tickflo.Core.Services.Widgets;

/// <summary>
/// The health state of a widget's most recent fetch.
/// </summary>
public enum WidgetHealth
{
    Ok = 1,
    Warning = 2,
    Critical = 3,
    Error = 4,
    Pending = 5,
}

/// <summary>
/// The last-known snapshot for a widget, held in memory by the polling service.
/// </summary>
public sealed record WidgetSnapshot(string? Value, WidgetHealth Health, DateTime? FetchedAt, string? ErrorMessage);
