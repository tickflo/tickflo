namespace Tickflo.Core.Services.Widgets;

using Tickflo.Core.Entities;

/// <summary>
/// Manages the widget catalog for a workspace: list, create, update, delete.
/// </summary>
public interface IWidgetService
{
    public Task<IReadOnlyList<Widget>> GetWidgetsForWorkspaceAsync(int workspaceId);

    public Task<IReadOnlyList<Widget>> GetEnabledWidgetsAsync();

    public Task<Widget?> GetWidgetAsync(int workspaceId, int widgetId);

    public Task<Widget> CreateWidgetAsync(
        int workspaceId,
        string name,
        WidgetType type,
        string url,
        string? secret,
        string configJson,
        int refreshIntervalSeconds,
        int sortOrder,
        int createdBy);

    public Task<Widget> UpdateWidgetAsync(
        int workspaceId,
        int widgetId,
        string name,
        WidgetType type,
        string url,
        string? secret,
        string configJson,
        int refreshIntervalSeconds,
        int sortOrder,
        int updatedBy);

    public Task DeleteWidgetAsync(int workspaceId, int widgetId);
}
