namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

public class WorkspaceTeamsEditViewData
{
    public bool CanViewTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public bool CanCreateTeams { get; set; }
    public Team? ExistingTeam { get; set; }
    public List<User> WorkspaceUsers { get; set; } = [];
    public List<int> ExistingMemberIds { get; set; } = [];
}

public interface IWorkspaceTeamsEditViewService
{
    public Task<WorkspaceTeamsEditViewData> BuildAsync(int workspaceId, int userId, int teamId = 0);
}


public class WorkspaceTeamsEditViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceTeamsEditViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceTeamsEditViewData> BuildAsync(int workspaceId, int userId, int teamId = 0)
    {
        var data = new WorkspaceTeamsEditViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewTeams = data.CanEditTeams = data.CanCreateTeams = true;
        }
        else if (permissions.TryGetValue("teams", out var tp))
        {
            data.CanViewTeams = tp.CanView;
            data.CanEditTeams = tp.CanEdit;
            data.CanCreateTeams = tp.CanCreate;
        }

        // Load workspace users
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

        if (teamId > 0)
        {
            data.ExistingTeam = await this.dbContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (data.ExistingTeam != null)
            {
                data.ExistingMemberIds = await this.dbContext.TeamMembers
                    .AsNoTracking()
                    .Where(tm => tm.TeamId == teamId)
                    .Select(tm => tm.UserId)
                    .ToListAsync();
            }
        }

        return data;
    }
}
