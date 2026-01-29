namespace Tickflo.Core.Services.Teams;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Service for managing teams and team member assignments.
/// </summary>

/// <summary>
/// Service for managing teams and team member assignments.
/// </summary>
public interface ITeamManagementService
{
    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Team name</param>
    /// <param name="description">Team description</param>
    /// <returns>Created team</returns>
    public Task<Team> CreateTeamAsync(int workspaceId, string name, string? description = null);

    /// <summary>
    /// Updates an existing team.
    /// </summary>
    /// <param name="teamId">Team to update</param>
    /// <param name="name">New name</param>
    /// <param name="description">New description</param>
    /// <returns>Updated team</returns>
    public Task<Team> UpdateTeamAsync(int teamId, string name, string? description = null);

    /// <summary>
    /// Deletes a team.
    /// </summary>
    /// <param name="teamId">Team to delete</param>
    public Task DeleteTeamAsync(int teamId);

    /// <summary>
    /// Synchronizes team member assignments (adds new, removes old).
    /// </summary>
    /// <param name="teamId">Team to update</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="memberUserIds">Current member user IDs</param>
    public Task SyncTeamMembersAsync(int teamId, int workspaceId, List<int> memberUserIds);

    /// <summary>
    /// Validates team name uniqueness within a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Team name to check</param>
    /// <param name="excludeTeamId">Optional team ID to exclude</param>
    /// <returns>True if name is unique</returns>
    public Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeTeamId = null);

    /// <summary>
    /// Validates that all user IDs are members of the workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="userIds">User IDs to validate</param>
    /// <returns>True if all users are valid members</returns>
    public Task<bool> ValidateMembersAsync(int workspaceId, List<int> userIds);
}

public class TeamManagementService(TickfloDbContext dbContext) : ITeamManagementService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<Team> CreateTeamAsync(int workspaceId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Team name is required");
        }

        var trimmedName = name.Trim();

        if (!await this.IsNameUniqueAsync(workspaceId, trimmedName))
        {
            throw new InvalidOperationException($"Team '{trimmedName}' already exists");
        }

        var team = new Team
        {
            WorkspaceId = workspaceId,
            Name = trimmedName,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        this.dbContext.Teams.Add(team);
        await this.dbContext.SaveChangesAsync();

        return team;
    }

    public async Task<Team> UpdateTeamAsync(int teamId, string name, string? description = null)
    {
        var team = await this.dbContext.Teams.FindAsync(teamId) ?? throw new InvalidOperationException("Team not found");

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Team name is required");
        }

        var trimmedName = name.Trim();

        if (trimmedName != team.Name && !await this.IsNameUniqueAsync(team.WorkspaceId, trimmedName, teamId))
        {
            throw new InvalidOperationException($"Team '{trimmedName}' already exists");
        }

        team.Name = trimmedName;
        team.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        team.UpdatedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync();

        return team;
    }

    public async Task DeleteTeamAsync(int teamId)
    {
        var team = await this.dbContext.Teams.FindAsync(teamId) ?? throw new InvalidOperationException("Team not found");

        this.dbContext.Teams.Remove(team);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task SyncTeamMembersAsync(int teamId, int workspaceId, List<int> memberUserIds)
    {
        var team = await this.dbContext.Teams.FindAsync(teamId) ?? throw new InvalidOperationException("Team not found");

        if (team.WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException("Team does not belong to workspace");
        }

        // Validate all users are workspace members
        if (!await this.ValidateMembersAsync(workspaceId, memberUserIds))
        {
            throw new InvalidOperationException("One or more users are not workspace members");
        }

        // Get current members
        var currentMembers = await this.dbContext.TeamMembers
            .Where(tm => tm.TeamId == teamId)
            .ToListAsync();
        var currentUserIds = currentMembers.Select(m => m.UserId).ToHashSet();

        var newUserIds = memberUserIds.ToHashSet();

        // Remove members not in new list
        var toRemove = currentUserIds.Except(newUserIds);
        var membersToRemove = currentMembers.Where(m => toRemove.Contains(m.UserId));
        this.dbContext.TeamMembers.RemoveRange(membersToRemove);

        // Add new members
        var toAdd = newUserIds.Except(currentUserIds);
        foreach (var userId in toAdd)
        {
            this.dbContext.TeamMembers.Add(new TeamMember
            {
                TeamId = teamId,
                UserId = userId
            });
        }

        await this.dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeTeamId = null)
    {
        var existing = await this.dbContext.Teams
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        return existing == null || (excludeTeamId.HasValue && existing.Id == excludeTeamId.Value);
    }

    public async Task<bool> ValidateMembersAsync(int workspaceId, List<int> userIds)
    {
        var validUserIds = await this.dbContext.UserWorkspaces
            .Where(uw => uw.WorkspaceId == workspaceId && uw.Accepted)
            .Select(uw => uw.UserId)
            .ToListAsync();

        var validUserIdSet = validUserIds.ToHashSet();

        return userIds.All(validUserIdSet.Contains);
    }
}


