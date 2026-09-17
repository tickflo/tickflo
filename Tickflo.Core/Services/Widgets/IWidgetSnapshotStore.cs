namespace Tickflo.Core.Services.Widgets;

using System.Collections.Concurrent;

/// <summary>
/// In-memory store of the latest health snapshot per widget.
/// Populated by the polling service and read by the dashboard page.
/// </summary>
public interface IWidgetSnapshotStore
{
    public WidgetSnapshot? GetSnapshot(int widgetId);

    public void SetSnapshot(int widgetId, WidgetSnapshot snapshot);

    public void RemoveSnapshot(int widgetId);
}

public sealed class WidgetSnapshotStore : IWidgetSnapshotStore
{
    private readonly ConcurrentDictionary<int, WidgetSnapshot> snapshots = new();

    public WidgetSnapshot? GetSnapshot(int widgetId) =>
        this.snapshots.TryGetValue(widgetId, out var snapshot) ? snapshot : null;

    public void SetSnapshot(int widgetId, WidgetSnapshot snapshot) =>
        this.snapshots[widgetId] = snapshot;

    public void RemoveSnapshot(int widgetId) =>
        this.snapshots.TryRemove(widgetId, out _);
}
