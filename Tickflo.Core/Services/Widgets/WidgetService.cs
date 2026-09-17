namespace Tickflo.Core.Services.Widgets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;

public class WidgetService(TickfloDbContext dbContext, IWidgetSecretProtector secretProtector) : IWidgetService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWidgetSecretProtector secretProtector = secretProtector;

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

    public async Task<Widget> CreateWidgetAsync(
        int workspaceId,
        string name,
        WidgetType type,
        string url,
        string? secret,
        string configJson,
        int refreshIntervalSeconds,
        int sortOrder,
        int createdBy)
    {
        var widget = new Widget
        {
            WorkspaceId = workspaceId,
            Name = name,
            Type = type,
            Url = url,
            ApiKeyCiphertext = this.EncryptSecret(secret),
            ConfigJson = configJson,
            RefreshIntervalSeconds = refreshIntervalSeconds,
            SortOrder = sortOrder,
            CreatedBy = createdBy,
        };

        this.dbContext.Widgets.Add(widget);
        await this.dbContext.SaveChangesAsync();
        return widget;
    }

    public async Task<Widget> UpdateWidgetAsync(
        int workspaceId,
        int widgetId,
        string name,
        WidgetType type,
        string url,
        string? secret,
        string configJson,
        int refreshIntervalSeconds,
        int sortOrder,
        int updatedBy)
    {
        var widget = await this.dbContext.Widgets
            .FirstOrDefaultAsync(widget => widget.WorkspaceId == workspaceId && widget.Id == widgetId)
            ?? throw new NotFoundException("Widget not found.");

        widget.Name = name;
        widget.Type = type;
        widget.Url = url;
        widget.ConfigJson = configJson;
        widget.RefreshIntervalSeconds = refreshIntervalSeconds;
        widget.SortOrder = sortOrder;
        widget.UpdatedAt = DateTime.UtcNow;
        widget.UpdatedBy = updatedBy;

        if (!string.IsNullOrWhiteSpace(secret))
        {
            widget.ApiKeyCiphertext = this.EncryptSecret(secret);
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
    }

    private string? EncryptSecret(string? secret) =>
        string.IsNullOrWhiteSpace(secret) ? null : this.secretProtector.Protect(secret);
}
