namespace Tickflo.Web.Services;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;

// TODO: This is a TEMPORARY service that should NOT exist. This logic belongs in Tickflo.Core
// This is only here as a stopgap while migrating away from repositories

public interface ITempRolePermissionService
{
    public Task<Dictionary<string, EffectiveSectionPermission>> GetEffectivePermissionsForUserAsync(int workspaceId, int userId);
    public Task UpsertAsync(int roleId, IEnumerable<EffectiveSectionPermission> permissions, int userId);
}

public class TempRolePermissionService(TickfloDbContext dbContext) : ITempRolePermissionService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<Dictionary<string, EffectiveSectionPermission>> GetEffectivePermissionsForUserAsync(int workspaceId, int userId)
    {
        // Get all roles for the user in this workspace
        var userRoleIds = await this.dbContext.UserWorkspaceRoles
            .Where(ur => ur.WorkspaceId == workspaceId && ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (userRoleIds.Count == 0)
        {
            return new Dictionary<string, EffectiveSectionPermission>(StringComparer.OrdinalIgnoreCase);
        }

        // Get all permissions for these roles
        var permissions = await this.dbContext.RolePermissionsTable
            .Where(rp => userRoleIds.Contains(rp.RoleId))
            .ToListAsync();

        // Aggregate permissions by section (taking the most permissive)
        var result = new Dictionary<string, EffectiveSectionPermission>(StringComparer.OrdinalIgnoreCase);
        foreach (var perm in permissions)
        {
            if (!result.TryGetValue(perm.Section, out var existing))
            {
                result[perm.Section] = new EffectiveSectionPermission
                {
                    Section = perm.Section,
                    CanView = perm.CanView,
                    CanEdit = perm.CanEdit,
                    CanCreate = perm.CanCreate,
                    TicketViewScope = perm.TicketViewScope
                };
            }
            else
            {
                existing.CanView = existing.CanView || perm.CanView;
                existing.CanEdit = existing.CanEdit || perm.CanEdit;
                existing.CanCreate = existing.CanCreate || perm.CanCreate;

                // For ticket view scope, "all" is most permissive, then "team", then "mine"
                if (perm.TicketViewScope == "all" || existing.TicketViewScope == null)
                {
                    existing.TicketViewScope = perm.TicketViewScope;
                }
                else if (perm.TicketViewScope == "team" && existing.TicketViewScope == "mine")
                {
                    existing.TicketViewScope = "team";
                }
            }
        }

        return result;
    }

    public async Task UpsertAsync(int roleId, IEnumerable<EffectiveSectionPermission> permissions, int userId)
    {
        // Remove existing permissions for this role
        var existing = await this.dbContext.RolePermissionsTable
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
        this.dbContext.RolePermissionsTable.RemoveRange(existing);

        // Add new permissions
        foreach (var perm in permissions)
        {
            this.dbContext.RolePermissionsTable.Add(new Core.Entities.RolePermission
            {
                RoleId = roleId,
                Section = perm.Section,
                CanView = perm.CanView,
                CanEdit = perm.CanEdit,
                CanCreate = perm.CanCreate,
                TicketViewScope = perm.TicketViewScope
            });
        }

        await this.dbContext.SaveChangesAsync();
    }
}
