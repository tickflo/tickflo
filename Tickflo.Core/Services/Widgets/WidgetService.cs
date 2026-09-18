namespace Tickflo.Core.Services.Widgets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;

public class WidgetService(TickfloDbContext dbContext, IWidgetSecretProtector secretProtector, IWidgetSnapshotStore snapshotStore) : IWidgetService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWidgetSecretProtector secretProtector = secretProtector;
    private readonly IWidgetSnapshotStore snapshotStore = snapshotStore;

    public async Task<IReadOnlyList<Widget>> GetWidgetsForWorkspaceAsync(int workspaceId) =>
        await this.dbContext.Widgets
            .AsNoTracking()
            .Where(widget => widget.WorkspaceId == workspaceId)
            .OrderBy(widget => widget.SortOrder)
            .ThenBy(widget => widget.Id)
            .ToListAsync();

    public async Task<IReadOnlyList<Widget>> GetEnabledWidgetsAsync() =>
        await this.dbContext.Widgets
            .AsNoTracking()
            .Where(widget => widget.IsEnabled)
            .ToListAsync();

    public async Task<Widget?> GetWidgetAsync(int workspaceId, int widgetId) =>
        await this.dbContext.Widgets
            .AsNoTracking()
            .FirstOrDefaultAsync(widget => widget.WorkspaceId == workspaceId && widget.Id == widgetId);

    public async Task<Widget> CreateWidgetAsync(int workspaceId, WidgetDraft draft, int createdBy)
    {
        var widget = new Widget
        {
            WorkspaceId = workspaceId,
            Name = draft.Name,
            Type = draft.Type,
            Url = draft.Url,
            ApiKeyCiphertext = this.EncryptSecret(draft.Secret),
            ConfigJson = draft.ConfigJson,
            RefreshIntervalSeconds = draft.RefreshIntervalSeconds,
            SortOrder = draft.SortOrder,
            IsEnabled = draft.IsEnabled,
            CreatedBy = createdBy,
        };

        this.dbContext.Widgets.Add(widget);
        await this.dbContext.SaveChangesAsync();
        return widget;
    }

    public async Task<Widget> UpdateWidgetAsync(int workspaceId, int widgetId, WidgetDraft draft, int updatedBy)
    {
        var widget = await this.dbContext.Widgets
            .FirstOrDefaultAsync(widget => widget.WorkspaceId == workspaceId && widget.Id == widgetId)
            ?? throw new NotFoundException("Widget not found.");

        widget.Name = draft.Name;
        widget.Type = draft.Type;
        widget.Url = draft.Url;
        widget.ConfigJson = draft.ConfigJson;
        widget.RefreshIntervalSeconds = draft.RefreshIntervalSeconds;
        widget.SortOrder = draft.SortOrder;
        widget.IsEnabled = draft.IsEnabled;
        widget.UpdatedAt = DateTime.UtcNow;
        widget.UpdatedBy = updatedBy;

        if (!string.IsNullOrWhiteSpace(draft.Secret))
        {
            // A newly typed secret always wins over "clear", so saving a password
            // cannot accidentally wipe an existing one.
            widget.ApiKeyCiphertext = this.EncryptSecret(draft.Secret);
        }
        else if (draft.ClearSecret)
        {
            widget.ApiKeyCiphertext = null;
        }

        await this.dbContext.SaveChangesAsync();
        return widget;
    }

    public async Task DeleteWidgetAsync(int workspaceId, int widgetId)
    {
        var widget = await this.dbContext.Widgets
            .FirstOrDefaultAsync(widget => widget.WorkspaceId == workspaceId && widget.Id == widgetId)
            ?? throw new NotFoundException("Widget not found.");

        this.dbContext.Widgets.Remove(widget);
        await this.dbContext.SaveChangesAsync();
        this.snapshotStore.RemoveSnapshot(widgetId);
    }

    private string? EncryptSecret(string? secret) =>
        string.IsNullOrWhiteSpace(secret) ? null : this.secretProtector.Protect(secret);
}
