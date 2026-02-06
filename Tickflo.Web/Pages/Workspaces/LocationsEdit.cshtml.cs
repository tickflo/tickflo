namespace Tickflo.Web.Pages.Workspaces;

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

// Thin PageModel - orchestrates UI and delegates to Core services

[Authorize]
public class LocationsEditModel(
    IWorkspaceService workspaceService,
    IWorkspaceLocationsEditViewService workspaceLocationsEditViewService,
    ILocationSetupService locationSetupService) : WorkspacePageModel
{
    #region Constants
    private const int NewLocationId = 0;
    private static readonly CompositeFormat LocationCreatedSuccessfully = CompositeFormat.Parse("Location '{0}' created successfully.");
    private static readonly CompositeFormat LocationUpdatedSuccessfully = CompositeFormat.Parse("Location '{0}' updated successfully.");
    #endregion

    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceLocationsEditViewService workspaceLocationsEditViewService = workspaceLocationsEditViewService;
    private readonly ILocationSetupService locationSetupService = locationSetupService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int LocationId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Location name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    [BindProperty]
    public bool Active { get; set; } = true;

    [BindProperty]
    public int? DefaultAssigneeUserId { get; set; }

    public List<User> MemberOptions { get; private set; } = [];

    [BindProperty]
    public List<int> SelectedContactIds { get; set; } = [];

    public List<Contact> ContactOptions { get; private set; } = [];
    public bool CanViewLocations { get; private set; }
    public bool CanEditLocations { get; private set; }
    public bool CanCreateLocations { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int locationId = 0)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var workspaceId = this.Workspace.Id;

        var viewData = await this.workspaceLocationsEditViewService.BuildAsync(workspaceId, uid, locationId);
        this.CanViewLocations = viewData.CanViewLocations;
        this.CanEditLocations = viewData.CanEditLocations;
        this.CanCreateLocations = viewData.CanCreateLocations;

        if (this.EnsurePermissionOrForbid(this.CanViewLocations) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.MemberOptions = viewData.MemberOptions;
        this.ContactOptions = viewData.ContactOptions;

        if (locationId > NewLocationId)
        {
            this.LoadExistingLocationData(viewData);
        }
        else
        {
            this.InitializeNewLocationForm();
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var workspaceId = this.Workspace.Id;

        var viewData = await this.workspaceLocationsEditViewService.BuildAsync(workspaceId, uid, this.LocationId);
        if (this.EnsureCreateOrEditPermission(this.LocationId, viewData.CanCreateLocations, viewData.CanEditLocations) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (!this.ModelState.IsValid)
        {
            this.MemberOptions = viewData.MemberOptions;
            this.ContactOptions = viewData.ContactOptions;
            this.CanViewLocations = viewData.CanViewLocations;
            this.CanEditLocations = viewData.CanEditLocations;
            this.CanCreateLocations = viewData.CanCreateLocations;
            return this.Page();
        }

        try
        {
            if (this.LocationId == NewLocationId)
            {
                await this.CreateLocationAsync(workspaceId, uid);
            }
            else
            {
                await this.UpdateLocationAsync(workspaceId, uid);
            }
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
            this.MemberOptions = viewData.MemberOptions;
            this.ContactOptions = viewData.ContactOptions;
            this.CanViewLocations = viewData.CanViewLocations;
            this.CanEditLocations = viewData.CanEditLocations;
            this.CanCreateLocations = viewData.CanCreateLocations;
            return this.Page();
        }

        return this.RedirectToLocationsWithPreservedFilters(slug);
    }

    private void LoadExistingLocationData(WorkspaceLocationsEditViewData viewData)
    {
        if (viewData.ExistingLocation == null)
        {
            return;
        }

        this.LocationId = viewData.ExistingLocation.Id;
        this.Name = viewData.ExistingLocation.Name ?? string.Empty;
        this.Address = viewData.ExistingLocation.Address ?? string.Empty;
        this.Active = viewData.ExistingLocation.Active;
        this.DefaultAssigneeUserId = viewData.ExistingLocation.DefaultAssigneeUserId;
        this.SelectedContactIds = viewData.SelectedContactIds;
    }

    private void InitializeNewLocationForm()
    {
        this.LocationId = NewLocationId;
        this.Name = string.Empty;
        this.Address = string.Empty;
        this.Active = true;
        this.DefaultAssigneeUserId = null;
        this.SelectedContactIds = [];
    }

    private async Task CreateLocationAsync(int workspaceId, int userId)
    {
        var created = await this.locationSetupService.CreateLocationAsync(workspaceId, new LocationCreationRequest
        {
            Name = this.Name?.Trim() ?? string.Empty,
            Address = this.Address?.Trim() ?? string.Empty,
            Active = this.Active,
            DefaultAssigneeUserId = this.DefaultAssigneeUserId,
            ContactIds = this.SelectedContactIds ?? []
        }, userId);

        this.SetSuccessMessage(string.Format(null, LocationCreatedSuccessfully, created.Name));
    }

    private async Task UpdateLocationAsync(int workspaceId, int userId)
    {
        var updated = await this.locationSetupService.UpdateLocationDetailsAsync(workspaceId, this.LocationId, new LocationUpdateRequest
        {
            Name = this.Name?.Trim() ?? string.Empty,
            Address = this.Address?.Trim() ?? string.Empty,
            Active = this.Active,
            DefaultAssigneeUserId = this.DefaultAssigneeUserId,
            ContactIds = this.SelectedContactIds ?? []
        }, userId);

        this.SetSuccessMessage(string.Format(null, LocationUpdatedSuccessfully, updated.Name));
    }

    private RedirectToPageResult RedirectToLocationsWithPreservedFilters(string slug)
    {
        var queryQ = this.Request.Query["Query"].ToString();
        var pageQ = this.Request.Query["PageNumber"].ToString();
        return this.RedirectToPage("/Workspaces/Locations", new { slug, Query = queryQ, PageNumber = pageQ });
    }
}

