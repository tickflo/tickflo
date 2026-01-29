namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Workspace;

/// <summary>
/// Implementation of workspace users view service.
/// </summary>
/// <summary>
/// Service for aggregating workspace users/invites view data.
/// </summary>
public interface IWorkspaceUsersViewService
{
    public Task<WorkspaceUsersViewData> BuildAsync(int workspaceId, int currentUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated view data for workspace users page.
/// </summary>
public class WorkspaceUsersViewData
{
    public bool IsWorkspaceAdmin { get; set; }
    public bool CanViewUsers { get; set; }
    public bool CanCreateUsers { get; set; }
    public bool CanEditUsers { get; set; }
    public List<InviteView> PendingInvites { get; set; } = [];
    public List<AcceptedUserView> AcceptedUsers { get; set; } = [];
}

public class InviteView
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Roles { get; set; } = [];
}

public class AcceptedUserView
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsAdmin { get; set; }
}

public class WorkspaceUsersViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceUsersViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceUsersViewData> BuildAsync(int workspaceId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var data = new WorkspaceUsersViewData
        {
            // Determine admin and permissions
            IsWorkspaceAdmin = currentUserId > 0 && await this.workspaceAccessService.UserIsWorkspaceAdminAsync(currentUserId, workspaceId)
        };
        if (currentUserId > 0)
        {
            var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, currentUserId);
            if (permissions.TryGetValue("users", out var up))
            {
                data.CanViewUsers = up.CanView || data.IsWorkspaceAdmin;
                data.CanCreateUsers = up.CanCreate || data.IsWorkspaceAdmin;
                data.CanEditUsers = up.CanEdit || data.IsWorkspaceAdmin;
            }
            else
            {
                data.CanViewUsers = data.IsWorkspaceAdmin;
                data.CanCreateUsers = data.IsWorkspaceAdmin;
                data.CanEditUsers = data.IsWorkspaceAdmin;
            }
        }

        // Build pending invites
        var memberships = await this.dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(uw => uw.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        var allUserIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var users = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => allUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

        foreach (var membership in memberships.Where(m => !m.Accepted))
        {
            if (!users.TryGetValue(membership.UserId, out var user))
            {
                continue;
            }

            var roleNames = await this.dbContext.UserWorkspaceRoles
                .AsNoTracking()
                .Where(uwr => uwr.UserId == user.Id && uwr.WorkspaceId == workspaceId)
                .Include(uwr => uwr.Role)
                .Select(uwr => uwr.Role.Name)
                .ToListAsync(cancellationToken);

            data.PendingInvites.Add(new InviteView
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roleNames
            });
        }

        // Build accepted users
        foreach (var membership in memberships.Where(m => m.Accepted))
        {
            if (!users.TryGetValue(membership.UserId, out var user))
            {
                continue;
            }

            var roleNames = await this.dbContext.UserWorkspaceRoles
                .AsNoTracking()
                .Where(uwr => uwr.UserId == user.Id && uwr.WorkspaceId == workspaceId)
                .Include(uwr => uwr.Role)
                .Select(uwr => uwr.Role.Name)
                .ToListAsync(cancellationToken);

            var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(user.Id, workspaceId);

            data.AcceptedUsers.Add(new AcceptedUserView
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name ?? string.Empty,
                JoinedAt = membership.UpdatedAt ?? membership.CreatedAt,
                Roles = roleNames,
                IsAdmin = isAdmin
            });
        }

        return data;
    }
}

