namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Widgets;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class DashboardEditModel(
    IWorkspaceService workspaceService,
    IWorkspaceAccessService workspaceAccessService,
    IWidgetService widgetService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IWidgetService widgetService = widgetService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public int WidgetId { get; private set; }
    public bool IsNew => this.WidgetId == 0;

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public WidgetType Type { get; set; } = WidgetType.HttpJson;

    [BindProperty]
    public string WidgetUrl { get; set; } = string.Empty;

    [BindProperty]
    public string? Secret { get; set; }

    [BindProperty]
    public string ConfigJson { get; set; } = "{}";

    [BindProperty]
    public int RefreshIntervalSeconds { get; set; } = 60;

    [BindProperty]
    public int SortOrder { get; set; }

    public List<SelectListItem> TypeOptions { get; } =
    [
        new SelectListItem("HTTP / JSON", ((int)WidgetType.HttpJson).ToString()),
        new SelectListItem("Wazuh", ((int)WidgetType.Wazuh).ToString()),
    ];

    private int CurrentUserId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, int id)
    {
        this.WorkspaceSlug = slug;
        this.WidgetId = id;

        var gate = await this.GateAsync(slug);
        if (gate is not null)
        {
            return gate;
        }

        if (id > 0)
        {
            var widget = await this.widgetService.GetWidgetAsync(this.Workspace!.Id, id);
            if (widget == null)
            {
                return this.NotFound();
            }

            this.Name = widget.Name;
            this.Type = widget.Type;
            this.WidgetUrl = widget.Url;
            this.ConfigJson = widget.ConfigJson;
            this.RefreshIntervalSeconds = widget.RefreshIntervalSeconds;
            this.SortOrder = widget.SortOrder;
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id)
    {
        this.WorkspaceSlug = slug;
        this.WidgetId = id;

        var gate = await this.GateAsync(slug);
        if (gate is not null)
        {
            return gate;
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        if (id == 0)
        {
            await this.widgetService.CreateWidgetAsync(
                this.Workspace!.Id,
                this.Name.Trim(),
                this.Type,
                this.WidgetUrl.Trim(),
                this.Secret,
                this.ConfigJson,
                this.RefreshIntervalSeconds,
                this.SortOrder,
                this.CurrentUserId);
        }
        else
        {
            await this.widgetService.UpdateWidgetAsync(
                this.Workspace!.Id,
                id,
                this.Name.Trim(),
                this.Type,
                this.WidgetUrl.Trim(),
                this.Secret,
                this.ConfigJson,
                this.RefreshIntervalSeconds,
                this.SortOrder,
                this.CurrentUserId);
        }

        this.SetSuccessMessage("Widget saved.");
        return this.RedirectToPage("Dashboard", new { slug = this.WorkspaceSlug });
    }

    private async Task<IActionResult?> GateAsync(string slug)
    {
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        this.CurrentUserId = userId;

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, this.Workspace.Id);
        if (!isAdmin)
        {
            return this.Forbid();
        }

        return null;
    }
}
