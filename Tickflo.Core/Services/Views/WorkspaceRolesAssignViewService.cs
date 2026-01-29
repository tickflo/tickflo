namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

public class WorkspaceRolesAssignViewData
{
    public bool IsAdmin { get; set; }
    public List<User> Members { get; set; } = [];
    public List<Role> Roles { get; set; } = [];
    public Dictionary<int, List<Role>> UserRoles { get; set; } = [];
}

public interface IWorkspaceRolesAssignViewService
{
    public Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceRolesAssignViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceRolesAssignViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceRolesAssignViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;
        if (!isAdmin)
        {
            return data;
        }

        var userIds = await this.dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(uw => uw.WorkspaceId == workspaceId)
            .Select(uw => uw.UserId)
            .Distinct()
            .ToListAsync();

        data.Members = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        data.Roles = await this.dbContext.Roles
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId)
            .ToListAsync();

        foreach (var id in userIds)
        {
            var roles = await this.dbContext.UserWorkspaceRoles
                .AsNoTracking()
                .Where(uwr => uwr.UserId == id && uwr.WorkspaceId == workspaceId)
                .Include(uwr => uwr.Role)
                .Select(uwr => uwr.Role)
                .ToListAsync();
            data.UserRoles[id] = roles;
        }

        return data;
    }
}


