namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

public class WorkspaceTeamsAssignViewData
{
    public bool CanViewTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public Team? Team { get; set; }
    public List<User> WorkspaceUsers { get; set; } = [];
    public List<User> Members { get; set; } = [];
}

public interface IWorkspaceTeamsAssignViewService
{
    public Task<WorkspaceTeamsAssignViewData> BuildAsync(int workspaceId, int userId, int teamId);
}


public class WorkspaceTeamsAssignViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceTeamsAssignViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceTeamsAssignViewData> BuildAsync(int workspaceId, int userId, int teamId)
    {
        var data = new WorkspaceTeamsAssignViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanViewTeams = isAdmin || (permissions.TryGetValue("teams", out var tp) && tp.CanView);
        data.CanEditTeams = isAdmin || (permissions.TryGetValue("teams", out var tp2) && tp2.CanEdit);
        if (!data.CanViewTeams)
        {
            return data;
        }

        data.Team = await this.dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (data.Team == null || data.Team.WorkspaceId != workspaceId)
        {
            return data;
        }

        var memberUserIds = await this.dbContext.TeamMembers
            .AsNoTracking()
            .Where(tm => tm.TeamId == teamId)
            .Select(tm => tm.UserId)
            .ToListAsync();

        data.Members = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => memberUserIds.Contains(u.Id))
            .ToListAsync();

        var workspaceUserIds = await this.dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(uw => uw.WorkspaceId == workspaceId)
            .Select(uw => uw.UserId)
            .Distinct()
            .ToListAsync();

        data.WorkspaceUsers = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => workspaceUserIds.Contains(u.Id))
            .ToListAsync();

        return data;
    }
}


