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

                // Get role permission links
                var rolePermissionLinks = await this.dbContext.RolePermissions
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                // Get permission catalog entries
                var permissionIds = rolePermissionLinks.Select(rp => rp.PermissionId).ToList();
                var permissions = await this.dbContext.Permissions
                    .AsNoTracking()
                    .Where(p => permissionIds.Contains(p.Id))
                    .ToListAsync();

                // Build effective permissions by section
                var managedSections = new[] { "dashboard", "contacts", "inventory", "locations", "reports", "roles", "teams", "tickets", "users", "settings" };
                var effectivePermissions = new List<EffectiveSectionPermission>();

                foreach (var section in managedSections)
                {
                    var eff = new EffectiveSectionPermission
                    {
                        Section = section,
                        CanView = permissions.Any(p => p.Resource == section && p.Action == "view"),
                        CanEdit = permissions.Any(p => p.Resource == section && p.Action == "edit"),
                        CanCreate = permissions.Any(p => p.Resource == section && p.Action == "create"),
                        CanDelete = false
                    };

                    if (section == "tickets")
                    {
                        var scopes = permissions
                            .Where(p => p.Resource == "tickets_scope")
                            .Select(p => p.Action.ToLower())
                            .ToList();
                        eff.TicketViewScope = scopes.Contains("mine") ? "mine" : scopes.Contains("team") ? "team" : scopes.Contains("all") ? "all" : "all";
                    }

                    effectivePermissions.Add(eff);
                }

                data.ExistingPermissions = [.. effectivePermissions];
            }
        }

        return data;
    }
}


