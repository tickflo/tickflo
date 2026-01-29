namespace Tickflo.Core.Services.Roles;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of IRoleManagementService.
/// Manages role assignments and role operations.
/// </summary>

/// <summary>
/// Service for managing role assignments and role operations.
/// Centralizes role management business logic.
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Assigns a role to a user in a workspace.
    /// </summary>
    /// <param name="userId">The user to assign the role to</param>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to assign</param>
    /// <param name="assignedByUserId">The user performing the assignment (for auditing)</param>
    /// <returns>The created role assignment</returns>
    /// <exception cref="InvalidOperationException">If role doesn't belong to workspace or user already has role</exception>
    public Task<UserWorkspaceRole> AssignRoleToUserAsync(int userId, int workspaceId, int roleId, int assignedByUserId);

    /// <summary>
    /// Removes a role assignment from a user.
    /// </summary>
    /// <param name="userId">The user to remove the role from</param>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to remove</param>
    /// <returns>True if assignment was removed, false if not found</returns>
    public Task<bool> RemoveRoleFromUserAsync(int userId, int workspaceId, int roleId);

    /// <summary>
    /// Counts how many users have a specific role in a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to count assignments for</param>
    /// <returns>Number of users with this role</returns>
    public Task<int> CountRoleAssignmentsAsync(int workspaceId, int roleId);

    /// <summary>
    /// Verifies that a role belongs to a specific workspace.
    /// </summary>
    /// <param name="roleId">The role to verify</param>
    /// <param name="workspaceId">The workspace that should own the role</param>
    /// <returns>True if role belongs to workspace</returns>
    public Task<bool> RoleBelongsToWorkspaceAsync(int roleId, int workspaceId);

    /// <summary>
    /// Gets all roles for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace to list roles for</param>
    /// <returns>List of roles in the workspace</returns>
    public Task<List<Role>> GetWorkspaceRolesAsync(int workspaceId);

    /// <summary>
    /// Gets all roles assigned to a user in a workspace.
    /// </summary>
    /// <param name="userId">The user to get roles for</param>
    /// <param name="workspaceId">The workspace context</param>
    /// <returns>List of roles assigned to the user</returns>
    public Task<List<Role>> GetUserRolesAsync(int userId, int workspaceId);

    /// <summary>
    /// Ensures a role can be deleted (has no assignments).
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to check</param>
    /// <param name="roleName">The role name (for error messages)</param>
    /// <exception cref="InvalidOperationException">If role has assignments</exception>
    public Task EnsureRoleCanBeDeletedAsync(int workspaceId, int roleId, string roleName);
}

public class RoleManagementService(TickfloDbContext dbContext) : IRoleManagementService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<UserWorkspaceRole> AssignRoleToUserAsync(int userId, int workspaceId, int roleId, int assignedByUserId)
    {
        // Verify role belongs to workspace
        var role = await this.dbContext.Roles.FindAsync(roleId);
        if (role == null || role.WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException($"Role {roleId} does not belong to workspace {workspaceId}.");
        }

        // Add the assignment
        var assignment = new UserWorkspaceRole
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            RoleId = roleId,
            CreatedBy = assignedByUserId
        };

        this.dbContext.UserWorkspaceRoles.Add(assignment);
        await this.dbContext.SaveChangesAsync();

        return assignment;
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, int workspaceId, int roleId)
    {
        var assignment = await this.dbContext.UserWorkspaceRoles
            .FirstOrDefaultAsync(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId && uwr.RoleId == roleId);

        if (assignment == null)
        {
            return false;
        }

        this.dbContext.UserWorkspaceRoles.Remove(assignment);
        await this.dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountRoleAssignmentsAsync(int workspaceId, int roleId) =>
        await this.dbContext.UserWorkspaceRoles
            .CountAsync(uwr => uwr.WorkspaceId == workspaceId && uwr.RoleId == roleId);

    public async Task<bool> RoleBelongsToWorkspaceAsync(int roleId, int workspaceId)
    {
        var role = await this.dbContext.Roles.FindAsync(roleId);
        return role != null && role.WorkspaceId == workspaceId;
    }

    public async Task<List<Role>> GetWorkspaceRolesAsync(int workspaceId) =>
        await this.dbContext.Roles
            .Where(r => r.WorkspaceId == workspaceId)
            .ToListAsync();

    public async Task<List<Role>> GetUserRolesAsync(int userId, int workspaceId) =>
        await this.dbContext.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Include(uwr => uwr.Role)
            .Select(uwr => uwr.Role)
            .ToListAsync();

    public async Task EnsureRoleCanBeDeletedAsync(int workspaceId, int roleId, string roleName)
    {
        var assignCount = await this.CountRoleAssignmentsAsync(workspaceId, roleId);
        if (assignCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete role '{roleName}' while {assignCount} user(s) are assigned. Unassign them first.");
        }
    }
}


