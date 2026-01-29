namespace Tickflo.Web.Services;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

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
    private static readonly string[] ManagedSections = ["dashboard", "contacts", "inventory", "locations", "reports", "roles", "teams", "tickets", "users", "settings"];

    public async Task<Dictionary<string, EffectiveSectionPermission>> GetEffectivePermissionsForUserAsync(int workspaceId, int userId)
    {
        // Get all roles for the user in this workspace
        var roles = await this.dbContext.UserWorkspaceRoles
            .Where(ur => ur.WorkspaceId == workspaceId && ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync();

        var roleIds = roles.Select(r => r.Id).ToList();
        var isAdmin = roles.Any(r => r.Admin);

        var result = new Dictionary<string, EffectiveSectionPermission>(StringComparer.OrdinalIgnoreCase);

        if (isAdmin)
        {
            foreach (var section in ManagedSections)
            {
                result[section] = new EffectiveSectionPermission
                {
                    Section = section,
                    CanView = true,
                    CanEdit = true,
                    CanCreate = true,
                    TicketViewScope = section == "tickets" ? "all" : null
                };
            }
            return result;
        }

        // Get role permission links
        var rolePermissionLinks = await this.dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync();

        // Get permission catalog entries
        var permissionIds = rolePermissionLinks.Select(rp => rp.PermissionId).Distinct().ToList();
        var permissions = await this.dbContext.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync();

        // Build effective permissions by section
        foreach (var section in ManagedSections)
        {
            var eff = new EffectiveSectionPermission
            {
                Section = section,
                CanView = permissions.Any(p => p.Resource.Equals(section, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("view", StringComparison.OrdinalIgnoreCase)),
                CanEdit = permissions.Any(p => p.Resource.Equals(section, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("edit", StringComparison.OrdinalIgnoreCase)),
                CanCreate = permissions.Any(p => p.Resource.Equals(section, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("create", StringComparison.OrdinalIgnoreCase))
            };

            if (section == "tickets")
            {
                var scopes = permissions.Where(p => p.Resource.Equals("tickets_scope", StringComparison.OrdinalIgnoreCase)).Select(p => p.Action.ToLowerInvariant()).ToList();
                eff.TicketViewScope = scopes.Contains("mine") ? "mine" : scopes.Contains("team") ? "team" : "all";
            }

            result[section] = eff;
        }

        return result;
    }

    public async Task UpsertAsync(int roleId, IEnumerable<EffectiveSectionPermission> permissions, int userId)
    {
        // Ensure permission catalog rows exist, then set role links accordingly
        var catalog = await this.dbContext.Permissions.ToListAsync();

        // Build desired permission set for this role
        var desired = new List<int>();
        foreach (var permission in permissions)
        {
            var section = (permission.Section ?? string.Empty).ToLowerInvariant();
            if (!ManagedSections.Contains(section))
            {
                continue;
            }

            if (permission.CanView)
            {
                desired.Add(await this.EnsurePermissionIdAsync(catalog, section, "view"));
            }

            if (permission.CanEdit)
            {
                desired.Add(await this.EnsurePermissionIdAsync(catalog, section, "edit"));
            }

            if (permission.CanCreate)
            {
                desired.Add(await this.EnsurePermissionIdAsync(catalog, section, "create"));
            }

            if (section == "tickets")
            {
                var scope = string.IsNullOrWhiteSpace(permission.TicketViewScope) ? "all" : permission.TicketViewScope.ToLowerInvariant();
                desired.Add(await this.EnsurePermissionIdAsync(catalog, "tickets_scope", scope));
            }
        }

        // Current links for managed resources
        var currentLinks = await this.dbContext.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        var currentPerms = await this.dbContext.Permissions.Where(p => currentLinks.Select(cl => cl.PermissionId).Contains(p.Id)).ToListAsync();

        // Filter to links related to our managed resources to avoid touching unrelated permissions
        var managedCurrentLinkIds = currentLinks
            .Where(l => currentPerms.Any(p => ManagedSections.Contains(p.Resource.ToLowerInvariant()) || p.Resource.Equals("tickets_scope", StringComparison.OrdinalIgnoreCase)))
            .Select(l => l.PermissionId)
            .ToHashSet();

        var currentSet = managedCurrentLinkIds;
        var desiredSet = desired.ToHashSet();

        // Remove obsolete links
        var toRemove = currentLinks.Where(l => managedCurrentLinkIds.Contains(l.PermissionId) && !desiredSet.Contains(l.PermissionId)).ToList();
        if (toRemove.Count > 0)
        {
            this.dbContext.RolePermissions.RemoveRange(toRemove);
        }

        // Add new links
        var toAdd = desiredSet.Except(currentSet).ToList();
        foreach (var pid in toAdd)
        {
            this.dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = pid,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await this.dbContext.SaveChangesAsync();
    }

    private async Task<int> EnsurePermissionIdAsync(List<Permission> catalog, string resource, string action)
    {
        var found = catalog.FirstOrDefault(p => p.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase) && p.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        if (found != null)
        {
            return found.Id;
        }

        var permission = new Permission { Resource = resource, Action = action };
        this.dbContext.Permissions.Add(permission);
        await this.dbContext.SaveChangesAsync();
        catalog.Add(permission);
        return permission.Id;
    }
}
