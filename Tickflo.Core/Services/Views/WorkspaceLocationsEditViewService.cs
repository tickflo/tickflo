namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

public class WorkspaceLocationsEditViewData
{
    public bool CanViewLocations { get; set; }
    public bool CanEditLocations { get; set; }
    public bool CanCreateLocations { get; set; }
    public Location? ExistingLocation { get; set; }
    public List<int> SelectedContactIds { get; set; } = [];
    public List<User> MemberOptions { get; set; } = [];
    public List<Contact> ContactOptions { get; set; } = [];
}

public interface IWorkspaceLocationsEditViewService
{
    public Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0);
}


public class WorkspaceLocationsEditViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceLocationsEditViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0)
    {
        var data = new WorkspaceLocationsEditViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewLocations = data.CanEditLocations = data.CanCreateLocations = true;
        }
        else if (permissions.TryGetValue("locations", out var lp))
        {
            data.CanViewLocations = lp.CanView;
            data.CanEditLocations = lp.CanEdit;
            data.CanCreateLocations = lp.CanCreate;
        }

        // Load members for default assignee selection
        var memberships = await this.dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(uw => uw.WorkspaceId == workspaceId)
            .Select(uw => uw.UserId)
            .Distinct()
            .ToListAsync();

        var users = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => memberships.Contains(u.Id))
            .ToListAsync();
        data.MemberOptions = users;

        // Load all contacts
        var contacts = await this.dbContext.Contacts
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .ToListAsync();
        data.ContactOptions = [.. contacts];

        if (locationId > 0)
        {
            data.ExistingLocation = await this.dbContext.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId);
            if (data.ExistingLocation != null)
            {
                var selectedContactIds = await this.dbContext.ContactLocations
                    .AsNoTracking()
                    .Where(cl => cl.WorkspaceId == workspaceId && cl.LocationId == locationId)
                    .Select(cl => cl.ContactId)
                    .ToListAsync();
                data.SelectedContactIds = [.. selectedContactIds];
            }
        }
        else
        {
            data.ExistingLocation = new Location { WorkspaceId = workspaceId, Active = true };
            data.SelectedContactIds = [];
        }

        return data;
    }
}
