namespace Tickflo.Core.Services.Workspace;

using System.Text;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;

/// <summary>
/// Implementation of IWorkspaceAccessService.
/// Provides workspace access verification and permission checking.
/// </summary>

/// <summary>
/// Service for verifying user access and permissions within workspaces.
/// Centralizes authorization logic for workspace operations.
/// </summary>
public interface IWorkspaceAccessService
{
    /// <summary>
    /// Verifies that a user has access to a specific workspace.
    /// </summary>
    /// <param name="userId">The user to check</param>
    /// <param name="workspaceId">The workspace to verify access to</param>
    /// <returns>True if user has accepted membership in workspace</returns>
    public Task<bool> UserHasAccessAsync(int userId, int workspaceId);

    /// <summary>
    /// Verifies that a user is an admin of a specific workspace.
    /// </summary>
    /// <param name="userId">The user to check</param>
    /// <param name="workspaceId">The workspace to check admin status in</param>
    /// <returns>True if user is workspace admin</returns>
    public Task<bool> UserIsWorkspaceAdminAsync(int userId, int workspaceId);

    /// <summary>
    /// Gets effective permissions for a user in a workspace.
    /// Combines role-based permissions with admin override.
    /// </summary>
    /// <param name="workspaceId">The workspace to check permissions in</param>
    /// <param name="userId">The user to get permissions for</param>
    /// <returns>Dictionary mapping permission names to permission objects</returns>
    public Task<Dictionary<string, EffectiveSectionPermission>> GetUserPermissionsAsync(int workspaceId, int userId);

    /// <summary>
    /// Checks if a user can perform a specific action in a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="userId">The user to check</param>
    /// <param name="resourceType">The resource type (e.g., "tickets", "contacts")</param>
    /// <param name="action">The action type (e.g., "view", "create", "edit")</param>
    /// <returns>True if user can perform the action</returns>
    public Task<bool> CanUserPerformActionAsync(int workspaceId, int userId, string resourceType, string action);

    /// <summary>
    /// Gets the ticket view scope for a user (what tickets they can see).
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="userId">The user to check</param>
    /// <param name="isAdmin">Whether user is workspace admin</param>
    /// <returns>View scope label (e.g., "all", "assigned", "created")</returns>
    public Task<string> GetTicketViewScopeAsync(int workspaceId, int userId, bool isAdmin);

    /// <summary>
    /// Ensures user has admin access or throws UnauthorizedAccessException.
    /// </summary>
    /// <param name="userId">The user to verify</param>
    /// <param name="workspaceId">The workspace to verify admin access to</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user is not admin</exception>
    public Task EnsureAdminAccessAsync(int userId, int workspaceId);

    /// <summary>
    /// Ensures user has access to workspace or throws UnauthorizedAccessException.
    /// </summary>
    /// <param name="userId">The user to verify</param>
    /// <param name="workspaceId">The workspace to verify access to</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user has no access</exception>
    public Task EnsureWorkspaceAccessAsync(int userId, int workspaceId);
}

public class WorkspaceAccessService(TickfloDbContext dbContext) : IWorkspaceAccessService
{
    #region Constants
    private const string ViewAction = "view";
    private const string CreateAction = "create";
    private const string EditAction = "edit";
    private const string AllTicketsScope = "all";
    private static readonly CompositeFormat UserNotAdminErrorFormat = CompositeFormat.Parse("User {0} is not an admin of workspace {1}.");
    private static readonly CompositeFormat UserNoAccessErrorFormat = CompositeFormat.Parse("User {0} does not have access to workspace {1}.");
    #endregion

    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<bool> UserHasAccessAsync(int userId, int workspaceId)
    {
        var userWorkspace = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId);
        return userWorkspace?.Accepted ?? false;
    }

    public async Task<bool> UserIsWorkspaceAdminAsync(int userId, int workspaceId)
    {
        // Check if user has a role with admin permissions
        var hasAdminRole = await this.dbContext.UserWorkspaceRoles
            .Include(uwr => uwr.Role)
            .AnyAsync(uwr => uwr.UserId == userId &&
                            uwr.WorkspaceId == workspaceId &&
                            uwr.Role.IsAdmin);

        return hasAdminRole;
    }

    public async Task<Dictionary<string, EffectiveSectionPermission>> GetUserPermissionsAsync(int workspaceId, int userId)
    {
        // Get all role permissions for the user's roles in this workspace
        var rolePermissions = await this.dbContext.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Include(uwr => uwr.Role)
            .ThenInclude(r => r.RolePermissions)
            .SelectMany(uwr => uwr.Role.RolePermissions)
            .ToListAsync();

        // Group by section and aggregate permissions (OR logic - any role grants permission)
        var effectivePermissions = rolePermissions
            .GroupBy(rp => rp.Section)
            .ToDictionary(
                g => g.Key,
                g => new EffectiveSectionPermission
                {
                    Section = g.Key,
                    CanView = g.Any(p => p.CanView),
                    CanCreate = g.Any(p => p.CanCreate),
                    CanEdit = g.Any(p => p.CanEdit),
                    CanDelete = false
                });

        return effectivePermissions;
    }

    public async Task<bool> CanUserPerformActionAsync(int workspaceId, int userId, string resourceType, string action)
    {
        if (await this.UserIsWorkspaceAdminAsync(userId, workspaceId))
        {
            return true;
        }

        var permissions = await this.GetUserPermissionsAsync(workspaceId, userId);
        if (!permissions.TryGetValue(resourceType, out var permission))
        {
            return false;
        }

        return IsActionAllowed(permission, action);
    }

    public async Task<string> GetTicketViewScopeAsync(int workspaceId, int userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return AllTicketsScope;
        }

        // Get the most permissive ticket view scope from user's roles
        var rolePermissions = await this.dbContext.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Include(uwr => uwr.Role)
            .ThenInclude(r => r.RolePermissions)
            .SelectMany(uwr => uwr.Role.RolePermissions)
            .Where(rp => rp.Section == "tickets")
            .ToListAsync();

        // If user can view all tickets (CanView = true), return "all"
        if (rolePermissions.Any(p => p.CanView))
        {
            return AllTicketsScope;
        }

        // Otherwise, return limited scope based on role metadata
        // Default to "assigned" if no specific scope found
        return "assigned";
    }

    public async Task EnsureAdminAccessAsync(int userId, int workspaceId)
    {
        var isAdmin = await this.UserIsWorkspaceAdminAsync(userId, workspaceId);
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException(string.Format(null, UserNotAdminErrorFormat, userId, workspaceId));
        }
    }

    public async Task EnsureWorkspaceAccessAsync(int userId, int workspaceId)
    {
        var hasAccess = await this.UserHasAccessAsync(userId, workspaceId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(string.Format(null, UserNoAccessErrorFormat, userId, workspaceId));
        }
    }

    private static bool IsActionAllowed(EffectiveSectionPermission permission, string action) => action switch
    {
        ViewAction => permission.CanView,
        CreateAction => permission.CanCreate,
        EditAction => permission.CanEdit,
        _ => false
    };
}



