namespace Tickflo.Core.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles ticket update workflows.
/// </summary>
public interface ITicketUpdateService
{
    /// <summary>
    /// Updates ticket core information with change tracking.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to update</param>
    /// <param name="request">Update details</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated ticket</returns>
    public Task<Ticket> UpdateTicketInfoAsync(int workspaceId, int ticketId, TicketUpdateRequest request, int updatedByUserId);
}

public class TicketUpdateService(TickfloDbContext dbContext) : ITicketUpdateService
{
    private const string ErrorTicketNotFound = "Ticket not found";

    private readonly TickfloDbContext dbContext = dbContext;

    /// <summary>
    /// Updates ticket core information (subject, description, etc.).
    /// </summary>
    public async Task<Ticket> UpdateTicketInfoAsync(
        int workspaceId,
        int ticketId,
        TicketUpdateRequest request,
        int updatedByUserId)
    {
        var ticket = await this.GetTicketOrThrowAsync(workspaceId, ticketId);
        var changes = TrackTicketChanges(ticket, request);

        if (changes.Count != 0)
        {
            ApplyTicketChanges(ticket, request);
            ticket.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync();
            await this.LogChangesAsync(workspaceId, ticketId, updatedByUserId, changes);
        }

        return ticket;
    }

    private async Task<Ticket> GetTicketOrThrowAsync(int workspaceId, int ticketId)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId)
            ?? throw new InvalidOperationException(ErrorTicketNotFound);

        return ticket;
    }

    private static List<string> TrackTicketChanges(Ticket ticket, TicketUpdateRequest request)
    {
        var changes = new List<string>();

        if (ShouldUpdateSubject(ticket, request))
        {
            changes.Add($"Subject changed from '{ticket.Subject}' to '{request.Subject!.Trim()}'");
        }

        if (ShouldUpdateDescription(ticket, request))
        {
            changes.Add("Description updated");
        }

        if (ShouldUpdateContact(ticket, request))
        {
            changes.Add($"Contact changed from {ticket.ContactId} to {request.ContactId}");
        }

        if (ShouldUpdateLocation(ticket, request))
        {
            changes.Add($"Location changed from {ticket.LocationId} to {request.LocationId}");
        }

        return changes;
    }

    private static bool ShouldUpdateSubject(Ticket ticket, TicketUpdateRequest request) =>
        !string.IsNullOrWhiteSpace(request.Subject) && ticket.Subject != request.Subject.Trim();

    private static bool ShouldUpdateDescription(Ticket ticket, TicketUpdateRequest request) =>
        !string.IsNullOrWhiteSpace(request.Description) && ticket.Description != request.Description.Trim();

    private static bool ShouldUpdateContact(Ticket ticket, TicketUpdateRequest request) =>
        request.ContactId.HasValue && ticket.ContactId != request.ContactId.Value;

    private static bool ShouldUpdateLocation(Ticket ticket, TicketUpdateRequest request) =>
        request.LocationId.HasValue && ticket.LocationId != request.LocationId.Value;

    private static void ApplyTicketChanges(Ticket ticket, TicketUpdateRequest request)
    {
        if (ShouldUpdateSubject(ticket, request))
        {
            ticket.Subject = request.Subject!.Trim();
        }

        if (ShouldUpdateDescription(ticket, request))
        {
            ticket.Description = request.Description!.Trim();
        }

        if (ShouldUpdateContact(ticket, request))
        {
            ticket.ContactId = request.ContactId!.Value;
        }

        if (ShouldUpdateLocation(ticket, request))
        {
            ticket.LocationId = request.LocationId!.Value;
        }
    }

    private async Task LogChangesAsync(int workspaceId, int ticketId, int updatedByUserId, List<string> changes)
    {
        this.dbContext.TicketHistory.Add(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = updatedByUserId,
            Action = TicketHistoryAction.FieldChanged,
            Note = string.Join("; ", changes),
            CreatedAt = DateTime.UtcNow
        });
        await this.dbContext.SaveChangesAsync();
    }
}

/// <summary>
/// Request to update ticket information.
/// </summary>
public class TicketUpdateRequest
{
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int? ContactId { get; set; }
    public int? LocationId { get; set; }
}
