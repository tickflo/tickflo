namespace Tickflo.Web.Pages.Workspaces;

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    [Required, MinLength(1), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public WidgetType Type { get; set; } = WidgetType.HttpJson;

    [BindProperty]
    [Required, MaxLength(2000)]
    public string WidgetUrl { get; set; } = string.Empty;

    [BindProperty]
    public string? Secret { get; set; }

    [BindProperty]
    public bool ClearSecret { get; set; }

    /// <summary>
    /// Wazuh API username (maps to <c>apiUsername</c> / <c>indexerUsername</c> in
    /// <see cref="ConfigJson"/>). Left empty in the override for other sources.
    /// </summary>
    [BindProperty]
    [MaxLength(200)]
    public string? Username { get; set; }

    [BindProperty]
    public string ConfigJson { get; set; } = "{}";

    [BindProperty]
    [Range(10, 86400)]
    public int RefreshIntervalSeconds { get; set; } = 60;

    [BindProperty]
    [Range(0, 10000)]
    public int SortOrder { get; set; }

    [BindProperty]
    public bool IsEnabled { get; set; } = true;

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
            this.Username = this.ReadWazuhUsername(widget.ConfigJson);
            this.RefreshIntervalSeconds = widget.RefreshIntervalSeconds;
            this.SortOrder = widget.SortOrder;
            this.IsEnabled = widget.IsEnabled;
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

        var draft = new WidgetDraft(
            this.Name.Trim(),
            this.Type,
            this.WidgetUrl.Trim(),
            this.Secret,
            this.ClearSecret,
            this.ApplyWazuhUsername(this.ConfigJson),
            this.RefreshIntervalSeconds,
            this.SortOrder,
            this.IsEnabled);

        if (id == 0)
        {
            await this.widgetService.CreateWidgetAsync(this.Workspace!.Id, draft, this.CurrentUserId);
        }
        else
        {
            await this.widgetService.UpdateWidgetAsync(this.Workspace!.Id, id, draft, this.CurrentUserId);
        }

        this.SetSuccessMessage("Widget saved.");
        return this.RedirectToPage("Dashboard", new { slug = this.WorkspaceSlug });
    }

    private static readonly JsonSerializerOptions WidgetConfigOptions = new() { PropertyNameCaseInsensitive = true };

    private string? ReadWazuhUsername(string? configJson)
    {
        if (this.Type != WidgetType.Wazuh || string.IsNullOrWhiteSpace(configJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(configJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var metric = root.TryGetProperty("metric", out var metricElement) && metricElement.ValueKind == JsonValueKind.String
                ? metricElement.GetString()
                : null;
            var key = string.Equals(metric, "criticalAlerts", StringComparison.OrdinalIgnoreCase)
                ? "indexerUsername"
                : "apiUsername";

            if (root.TryGetProperty(key, out var usernameElement) && usernameElement.ValueKind == JsonValueKind.String)
            {
                return usernameElement.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string ApplyWazuhUsername(string configJson)
    {
        if (this.Type != WidgetType.Wazuh || string.IsNullOrWhiteSpace(this.Username))
        {
            return configJson;
        }

        JsonNode node;
        try
        {
            node = JsonNode.Parse(string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson) ?? new JsonObject();
        }
        catch (JsonException)
        {
            node = new JsonObject();
        }

        var config = node as JsonObject ?? [];
        var metric = config["metric"]?.GetValue<string>();
        var key = string.Equals(metric, "criticalAlerts", StringComparison.OrdinalIgnoreCase)
            ? "indexerUsername"
            : "apiUsername";

        config[key] = this.Username.Trim();
        return config.ToJsonString(WidgetConfigOptions);
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
