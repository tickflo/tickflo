namespace Tickflo.Core.Services.Widgets;

using Tickflo.Core.Entities;

/// <summary>
/// The editable fields for creating or updating a widget. Bundles the form input
/// so the service contract stays small as widget options grow.
/// </summary>
public sealed record WidgetDraft(
    string Name,
    WidgetType Type,
    string Url,
    string? Secret,
    bool ClearSecret,
    string ConfigJson,
    int RefreshIntervalSeconds,
    int SortOrder,
    bool IsEnabled);

/// <summary>
/// Manages the widget catalog for a workspace: list, create, update, delete.
/// </summary>
public interface IWidgetService
{
    public Task<IReadOnlyList<Widget>> GetWidgetsForWorkspaceAsync(int workspaceId);

    public Task<IReadOnlyList<Widget>> GetEnabledWidgetsAsync();

    public Task<Widget?> GetWidgetAsync(int workspaceId, int widgetId);

    public Task<Widget> CreateWidgetAsync(int workspaceId, WidgetDraft draft, int createdBy);

    public Task<Widget> UpdateWidgetAsync(int workspaceId, int widgetId, WidgetDraft draft, int updatedBy);

    public Task DeleteWidgetAsync(int workspaceId, int widgetId);
}
