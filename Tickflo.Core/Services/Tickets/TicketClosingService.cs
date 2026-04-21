namespace Tickflo.Core.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;

/// <summary>
/// Handles ticket closing and resolution workflows.
/// </summary>
public interface ITicketClosingService
{
    /// <summary>
    /// Closes a ticket with resolution note.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to close</param>
    /// <param name="resolutionNote">Resolution details</param>
    /// <param name="closedByUserId">User closing the ticket</param>
    /// <returns>The closed ticket</returns>
    public Task<Ticket> CloseTicketAsync(int workspaceId, int ticketId, string resolutionNote, int closedByUserId);

    /// <summary>
    /// Reopens a previously closed ticket.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to reopen</param>
    /// <param name="reason">Reason for reopening</param>
    /// <param name="reopenedByUserId">User reopening the ticket</param>
    /// <returns>The reopened ticket</returns>
    public Task<Ticket> ReopenTicketAsync(int workspaceId, int ticketId, string reason, int reopenedByUserId);
}

public class TicketClosingService(
    TickfloDbContext dbContext,
    INotificationTriggerService notificationTriggerService) : ITicketClosingService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly INotificationTriggerService notificationTriggerService = notificationTriggerService;

    /// <summary>
    /// Closes a ticket with a resolution note.
    /// </summary>
    public async Task<Ticket> CloseTicketAsync(
        int workspaceId,
        int ticketId,
        string resolutionNote,
        int closedByUserId)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found");

        // Resolve closed status ID
        var closedStatus = await this.dbContext.TicketStatuses
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.IsClosedState)
            ?? throw new InvalidOperationException("Closed status not found in workspace");

        // Business rule: Cannot close an already closed ticket
        if (ticket.StatusId == closedStatus.Id)
        {
            throw new InvalidOperationException("Ticket is already closed");
        }

        // Business rule: Resolution note is required when closing
        if (string.IsNullOrWhiteSpace(resolutionNote))
        {
            throw new InvalidOperationException("Resolution note is required when closing a ticket");
        }

        ticket.StatusId = closedStatus.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync();

        // Log the closure
        var history = new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = closedByUserId,
            Action = TicketHistoryAction.Closed,
            Note = $"Ticket closed. Resolution: {resolutionNote}",
            CreatedAt = DateTime.UtcNow
        };

        this.dbContext.TicketHistory.Add(history);
        await this.dbContext.SaveChangesAsync();

        await this.notificationTriggerService.NotifyTicketUpdatedAsync(
            workspaceId,
            ticket,
            closedByUserId,
            $"Ticket closed. Resolution: {resolutionNote}");

        return ticket;
    }

    /// <summary>
    /// Reopens a previously closed ticket.
    /// </summary>
    public async Task<Ticket> ReopenTicketAsync(
        int workspaceId,
        int ticketId,
        string reason,
        int reopenedByUserId)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found");

        var closedStatus = await this.dbContext.TicketStatuses
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.IsClosedState)
            ?? throw new InvalidOperationException("Closed status not found in workspace");

        // Business rule: Can only reopen closed tickets
        if (ticket.StatusId != closedStatus.Id)
        {
            throw new InvalidOperationException("Can only reopen closed tickets");
        }

        // Business rule: Reason is required for reopening
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Reason is required when reopening a ticket");
        }

        var openStatus = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId && !s.IsClosedState)
            .OrderBy(s => s.SortOrder)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No open status found in workspace");

        ticket.StatusId = openStatus.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync();

        // Log the reopening
        var history = new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = reopenedByUserId,
            Action = TicketHistoryAction.Reopened,
            Note = $"Ticket reopened. Reason: {reason}",
            CreatedAt = DateTime.UtcNow
        };

        this.dbContext.TicketHistory.Add(history);
        await this.dbContext.SaveChangesAsync();

        await this.notificationTriggerService.NotifyTicketUpdatedAsync(
            workspaceId,
            ticket,
            reopenedByUserId,
            $"Ticket reopened. Reason: {reason}");

        return ticket;
    }
}
