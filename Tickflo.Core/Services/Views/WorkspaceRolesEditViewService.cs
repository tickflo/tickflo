namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

public class WorkspaceRolesEditViewData
{
    public bool IsAdmin { get; set; }
    public Role? ExistingRole { get; set; }
    public List<EffectiveSectionPermission> ExistingPermissions { get; set; } = [];
}

public interface IWorkspaceRolesEditViewService
{
    public Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0);
}


public class WorkspaceRolesEditViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceRolesEditViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0)
    {
        var data = new WorkspaceRolesEditViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;

        if (!isAdmin)
        {
            return data;
        }

        if (roleId > 0)
        {
            var role = await this.dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role != null && role.WorkspaceId == workspaceId)
            {
                data.ExistingRole = role;
                var rolePermissions = await this.dbContext.RolePermissionsTable
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();
                data.ExistingPermissions = [.. rolePermissions
                    .Select(rp => new EffectiveSectionPermission
                    {
                        Section = rp.Section,
                        CanView = rp.CanView,
                        CanCreate = rp.CanCreate,
                        CanEdit = rp.CanEdit,
                        CanDelete = false,
                        TicketViewScope = rp.TicketViewScope
                    })];
            }
        }

        return data;
    }
}


