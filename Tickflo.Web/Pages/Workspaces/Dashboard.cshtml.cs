namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Widgets;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class DashboardModel(
    IWorkspaceService workspaceService,
    IWorkspaceAccessService workspaceAccessService,
    IWidgetService widgetService,
    IWidgetSnapshotStore widgetSnapshotStore) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IWidgetService widgetService = widgetService;
    private readonly IWidgetSnapshotStore widgetSnapshotStore = widgetSnapshotStore;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<WidgetTile> Widgets { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(userId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, this.Workspace.Id);
        if (!isAdmin)
        {
            return this.Forbid();
        }

        var widgets = await this.widgetService.GetWidgetsForWorkspaceAsync(this.Workspace.Id);
        this.Widgets = [.. widgets.Select(this.BuildTile)];

        return this.Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string slug, int id)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, this.Workspace.Id);
        if (!isAdmin)
        {
            return this.Forbid();
        }

        await this.widgetService.DeleteWidgetAsync(this.Workspace.Id, id);
        this.SetSuccessMessage("Widget deleted.");
        return this.RedirectToPage("Dashboard", new { slug = this.WorkspaceSlug });
    }

    private WidgetTile BuildTile(Widget widget)
    {
        var snapshot = this.widgetSnapshotStore.GetSnapshot(widget.Id);
        return new WidgetTile(
            widget.Id,
            widget.Name,
            widget.Type,
            snapshot?.Value,
            snapshot?.Health ?? WidgetHealth.Pending,
            snapshot?.FetchedAt,
            snapshot?.ErrorMessage);
    }
}

public sealed record WidgetTile(
    int Id,
    string Name,
    WidgetType Type,
    string? Value,
    WidgetHealth Health,
    DateTime? FetchedAt,
    string? ErrorMessage)
{
    public string BadgeClass => this.Health switch
    {
        WidgetHealth.Ok => "badge-success",
        WidgetHealth.Warning => "badge-warning",
        WidgetHealth.Critical => "badge-error",
        WidgetHealth.Error => "badge-error",
        WidgetHealth.Pending => throw new NotImplementedException(),
        _ => "badge-ghost",
    };

    public string BorderClass => this.Health switch
    {
        WidgetHealth.Critical => "border-error/40",
        WidgetHealth.Warning => "border-warning/40",
        WidgetHealth.Error => "border-error/40",
        WidgetHealth.Ok => throw new NotImplementedException(),
        WidgetHealth.Pending => throw new NotImplementedException(),
        _ => "border-white/10",
    };
}
