namespace Tickflo.Core.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of assigning tickets to users and teams.
/// </summary>

/// <summary>
/// Handles ticket assignment workflows.
/// </summary>
public interface ITicketAssignmentService
{
    /// <summary>
    /// Assigns a ticket to a specific user.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to assign</param>
    /// <param name="assigneeUserId">User to assign to</param>
    /// <param name="assignedByUserId">User performing the assignment</param>
    /// <returns>The updated ticket</returns>
    public Task<Ticket> AssignToUserAsync(int workspaceId, int ticketId, int assigneeUserId, int assignedByUserId);

    /// <summary>
    /// Assigns a ticket to a team.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to assign</param>
    /// <param name="teamId">Team to assign to</param>
    /// <param name="assignedByUserId">User performing the assignment</param>
    /// <returns>The updated ticket</returns>
    public Task<Ticket> AssignToTeamAsync(int workspaceId, int ticketId, int teamId, int assignedByUserId);

    /// <summary>
    /// Unassigns a ticket from its current user.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to unassign</param>
    /// <param name="unassignedByUserId">User performing the unassignment</param>
    /// <returns>The updated ticket</returns>
    public Task<Ticket> UnassignUserAsync(int workspaceId, int ticketId, int unassignedByUserId);

    /// <summary>
    /// Reassigns a ticket from one user to another with optional reason.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to reassign</param>
    /// <param name="newAssigneeUserId">New assignee</param>
    /// <param name="reassignedByUserId">User performing the reassignment</param>
    /// <param name="reason">Optional reason for reassignment</param>
    /// <returns>The updated ticket</returns>
    public Task<Ticket> ReassignAsync(int workspaceId, int ticketId, int newAssigneeUserId, int reassignedByUserId, string? reason = null);

    /// <summary>
    /// Automatically assigns a ticket based on rules (team round-robin, location default, etc.).
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to auto-assign</param>
    /// <param name="triggeredByUserId">User triggering auto-assignment</param>
    /// <returns>The updated ticket</returns>
    public Task<Ticket> AutoAssignAsync(int workspaceId, int ticketId, int triggeredByUserId);

    /// <summary>
    /// Updates a ticket's user assignment without validation (for UI operations where access is pre-validated).
    /// </summary>
    /// <param name="ticket">The ticket to update</param>
    /// <param name="newAssignedUserId">New assignee user ID (null to unassign)</param>
    /// <param name="updatedByUserId">User performing the update</param>
    /// <returns>Returns true if assignment changed, false otherwise</returns>
    public Task<bool> UpdateAssignmentAsync(Ticket ticket, int? newAssignedUserId, int updatedByUserId);
}

public class TicketAssignmentService(TickfloDbContext dbContext) : ITicketAssignmentService
{
    private readonly TickfloDbContext dbContext = dbContext;

    /// <summary>
    /// Assigns a ticket to a specific user.
    /// </summary>
    public async Task<Ticket> AssignToUserAsync(
        int workspaceId,
        int ticketId,
        int assigneeUserId,
        int assignedByUserId)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found");

        // Business rule: Validate user has access to workspace
        var userWorkspace = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == assigneeUserId && uw.WorkspaceId == workspaceId)
            ?? throw new InvalidOperationException("User does not have access to this workspace");

        if (!userWorkspace.Accepted)
        {
            throw new InvalidOperationException("User has not accepted workspace invitation");
        }

        var previousAssignee = ticket.AssignedUserId;

        ticket.AssignedUserId = assigneeUserId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync();

        // Log assignment change
        var history = new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = assignedByUserId,
            Action = "assigned",
            Note = $"Ticket assigned to user {assigneeUserId}" +
                   (previousAssignee.HasValue ? $" (was user {previousAssignee.Value})" : ""),
            CreatedAt = DateTime.UtcNow
        };

        this.dbContext.TicketHistories.Add(history);
        await this.dbContext.SaveChangesAsync();

        // Could add: Send notification to assignee, update team assignment, etc.

        return ticket;
    }

    /// <summary>
    /// Assigns a ticket to a team.
    /// </summary>
    public async Task<Ticket> AssignToTeamAsync(
        int workspaceId,
        int ticketId,
        int teamId,
        int assignedByUserId)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found");

        // Business rule: Validate team belongs to workspace
        var team = await this.dbContext.Teams.FindAsync(teamId)
            ?? throw new InvalidOperationException("Team not found");

        if (team.WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException("Team does not belong to this workspace");
        }

        var previousTeam = ticket.AssignedTeamId;

        ticket.AssignedTeamId = teamId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync();

        // Log team assignment
        var history = new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = assignedByUserId,
            Action = "team_assigned",
            Note = $"Ticket assigned to team {team.Name}" +
                   (previousTeam.HasValue ? $" (was team {previousTeam.Value})" : ""),
            CreatedAt = DateTime.UtcNow
        };

        this.dbContext.TicketHistories.Add(history);
        await this.dbContext.SaveChangesAsync();

        // Could add: Notify team members, round-robin assign within team, etc.

        return ticket;
    }

    /// <summary>
    /// Unassigns a ticket from its current user assignee.
    /// </summary>
    public async Task<Ticket> UnassignUserAsync(
        int workspaceId,
        int ticketId,
        int unassignedByUserId)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found");

        if (!ticket.AssignedUserId.HasValue)
        {
            return ticket; // Already unassigned
        }

        var previousAssignee = ticket.AssignedUserId.Value;

        ticket.AssignedUserId = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync();

        var history = new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = unassignedByUserId,
            Action = "unassigned",
            Note = $"Ticket unassigned from user {previousAssignee}",
            CreatedAt = DateTime.UtcNow
        };

        this.dbContext.TicketHistories.Add(history);
        await this.dbContext.SaveChangesAsync();

        return ticket;
    }

    /// <summary>
    /// Reassigns a ticket from one user to another.
    /// </summary>
    public async Task<Ticket> ReassignAsync(
        int workspaceId,
        int ticketId,
        int newAssigneeUserId,
        int reassignedByUserId,
        string? reason = null)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found");

        var previousAssignee = ticket.AssignedUserId;

        // Use the assign method for validation
        ticket = await this.AssignToUserAsync(workspaceId, ticketId, newAssigneeUserId, reassignedByUserId);

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var history = new TicketHistory
            {
                WorkspaceId = workspaceId,
                TicketId = ticketId,
                CreatedByUserId = reassignedByUserId,
                Action = "reassignment_note",
                Note = $"Reassignment reason: {reason}",
                CreatedAt = DateTime.UtcNow
            };

            this.dbContext.TicketHistories.Add(history);
            await this.dbContext.SaveChangesAsync();
        }

        return ticket;
    }

    /// <summary>
    /// Automatically assigns a ticket based on team round-robin or location default.
    /// </summary>
    public async Task<Ticket> AutoAssignAsync(
        int workspaceId,
        int ticketId,
        int triggeredByUserId)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found");

        // Business rule: Try team-based assignment first
        if (ticket.AssignedTeamId.HasValue)
        {
            var teamMembers = await this.dbContext.TeamMembers
                .Where(tm => tm.TeamId == ticket.AssignedTeamId.Value)
                .ToListAsync();

            if (teamMembers.Count != 0)
            {
                // Simple round-robin: assign to first available member
                // Could be enhanced with load balancing, availability checks, etc.
                var assignee = teamMembers.First();
                return await this.AssignToUserAsync(workspaceId, ticketId, assignee.UserId, triggeredByUserId);
            }
        }

        // Business rule: Fall back to location default if no team assignment
        // (This logic could be moved here from TicketManagementService)

        return ticket;
    }

    public async Task<bool> UpdateAssignmentAsync(Ticket ticket, int? newAssignedUserId, int updatedByUserId)
    {
        var oldAssignedUserId = ticket.AssignedUserId;
        var normalizedNewAssignedUserId = newAssignedUserId.HasValue && newAssignedUserId.Value > 0 ? newAssignedUserId : null;

        if (oldAssignedUserId == normalizedNewAssignedUserId)
        {
            return false;
        }

        ticket.AssignedUserId = normalizedNewAssignedUserId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await this.dbContext.SaveChangesAsync();

        return true;
    }
}
