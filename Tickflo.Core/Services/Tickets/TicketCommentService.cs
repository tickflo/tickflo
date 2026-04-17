namespace Tickflo.Core.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;

/// <summary>
/// Service for managing ticket comments with support for client-visible and internal-only comments.
/// Handles comment operations including creation, updates, retrieval, and deletion with proper access control.
/// </summary>
public interface ITicketCommentService
{
    /// <summary>
    /// Retrieves all comments for a given ticket, filtered by visibility based on the view context.
    /// Internal view returns all comments; client view returns only client-visible comments.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="ticketId">The ticket ID to retrieve comments for</param>
    /// <param name="isClientView">If true, filters to only client-visible comments; if false, returns all comments</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Read-only list of comments matching the visibility criteria, ordered by creation date</returns>
    public Task<IReadOnlyList<TicketComment>> GetCommentsAsync(int workspaceId, int ticketId, bool isClientView = false, CancellationToken ct = default);

    /// <summary>
    /// Creates a new comment on a ticket with specified visibility and audit trail.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping</param>
    /// <param name="ticketId">The ticket ID to add the comment to</param>
    /// <param name="createdByUserId">The user ID creating the comment (for audit trail)</param>
    /// <param name="content">The comment text (must be non-empty)</param>
    /// <param name="isVisibleToClient">If true, comment is visible to clients; if false, internal-only</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The newly created comment with assigned ID and timestamps</returns>
    /// <exception cref="InvalidOperationException">Thrown if content is empty or null</exception>
    public Task<TicketComment> AddCommentAsync(int workspaceId, int ticketId, int createdByUserId, string content, bool isVisibleToClient, CancellationToken ct = default);

    /// <summary>
    /// Creates a new comment and dispatches ticket notifications for that comment event.
    /// </summary>
    public Task<TicketComment> AddCommentAndNotifyAsync(int workspaceId, int ticketId, int createdByUserId, string content, bool isVisibleToClient, CancellationToken ct = default);

    /// <summary>
    /// Updates the content of an existing comment and records the update timestamp and user.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to update</param>
    /// <param name="content">The new comment content (must be non-empty)</param>
    /// <param name="updatedByUserId">The user ID performing the update (for audit trail)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The updated comment with new content and update metadata</returns>
    /// <exception cref="InvalidOperationException">Thrown if comment not found or content is empty</exception>
    public Task<TicketComment> UpdateCommentAsync(int workspaceId, int commentId, string content, int updatedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new comment on a ticket from a client with specified visibility.
    /// Automatically marks the comment as visible to client since it's submitted by the client.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping</param>
    /// <param name="ticketId">The ticket ID to add the comment to</param>
    /// <param name="contactId">The contact ID of the client creating the comment</param>
    /// <param name="content">The comment text (must be non-empty)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The newly created comment with assigned ID and timestamps</returns>
    /// <exception cref="InvalidOperationException">Thrown if content is empty or null</exception>
    public Task<TicketComment> AddClientCommentAsync(int workspaceId, int ticketId, int contactId, string content, CancellationToken ct = default);

    /// <summary>
    /// Deletes a comment from a ticket. This is a hard delete operation.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>If the comment does not exist, the operation completes silently (idempotent)</remarks>
    public Task DeleteCommentAsync(int workspaceId, int commentId, CancellationToken ct = default);
}

public class TicketCommentService(
    TickfloDbContext dbContext,
    INotificationTriggerService notificationTriggerService) : ITicketCommentService
{
    private const int SystemUserId = 1;

    private readonly TickfloDbContext dbContext = dbContext;
    private readonly INotificationTriggerService notificationTriggerService = notificationTriggerService;

    /// <summary>
    /// Retrieves comments for a ticket with visibility filtering based on view context.
    /// </summary>
    public async Task<IReadOnlyList<TicketComment>> GetCommentsAsync(int workspaceId, int ticketId, bool isClientView = false, CancellationToken ct = default)
    {
        ValidateIdentifiers(workspaceId, ticketId);

        var query = this.dbContext.TicketComments
            .Where(c => c.WorkspaceId == workspaceId && c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt);

        if (isClientView)
        {
            return await query
                .Where(c => c.IsVisibleToClient)
                .ToListAsync(ct);
        }

        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Creates a new comment with content validation and audit trail setup.
    /// </summary>
    public async Task<TicketComment> AddCommentAsync(int workspaceId, int ticketId, int createdByUserId, string content, bool isVisibleToClient, CancellationToken ct = default)
    {
        ValidateIdentifiers(workspaceId, ticketId);
        if (createdByUserId <= 0)
        {
            throw new InvalidOperationException("Invalid user ID for comment creator");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Comment content cannot be empty");
        }

        var comment = new TicketComment
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = createdByUserId,
            Content = content.Trim(),
            IsVisibleToClient = isVisibleToClient,
        };

        this.dbContext.TicketComments.Add(comment);
        await this.dbContext.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<TicketComment> AddCommentAndNotifyAsync(int workspaceId, int ticketId, int createdByUserId, string content, bool isVisibleToClient, CancellationToken ct = default)
    {
        var ticket = await this.dbContext.Tickets
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId, ct)
            ?? throw new InvalidOperationException("Ticket not found");
        var comment = await this.AddCommentAsync(workspaceId, ticketId, createdByUserId, content, isVisibleToClient, ct);

        await this.notificationTriggerService.NotifyTicketCommentAddedAsync(
            workspaceId,
            ticket,
            createdByUserId,
            isVisibleToClient);

        return comment;
    }

    /// <summary>
    /// Creates a new comment from a client on a ticket.
    /// Automatically marks as visible to client and records the contact ID.
    /// </summary>
    public async Task<TicketComment> AddClientCommentAsync(int workspaceId, int ticketId, int contactId, string content, CancellationToken ct = default)
    {
        ValidateIdentifiers(workspaceId, ticketId);
        if (contactId <= 0)
        {
            throw new InvalidOperationException("Invalid contact ID for comment creator");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Comment content cannot be empty");
        }

        // Client comments are attributed to the system user; the actual contact is tracked via CreatedByContactId.
        var comment = new TicketComment
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = SystemUserId,
            CreatedByContactId = contactId,
            Content = content.Trim(),
            IsVisibleToClient = true,
        };

        this.dbContext.TicketComments.Add(comment);
        await this.dbContext.SaveChangesAsync(ct);
        return comment;
    }

    /// <summary>
    /// Updates comment content with new audit trail metadata.
    /// </summary>
    public async Task<TicketComment> UpdateCommentAsync(int workspaceId, int commentId, string content, int updatedByUserId, CancellationToken ct = default)
    {
        ValidateIdentifiers(workspaceId, commentId);
        if (updatedByUserId <= 0)
        {
            throw new InvalidOperationException("Invalid user ID for comment updater");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Comment content cannot be empty");
        }

        var comment = await this.dbContext.TicketComments
            .FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == commentId, ct)
            ?? throw new InvalidOperationException($"Comment {commentId} not found in workspace {workspaceId}");

        comment.Content = content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        comment.UpdatedByUserId = updatedByUserId;

        await this.dbContext.SaveChangesAsync(ct);
        return comment;
    }

    /// <summary>
    /// Deletes a comment from the system.
    /// Operation is idempotent - if comment doesn't exist, no error is thrown.
    /// </summary>
    public async Task DeleteCommentAsync(int workspaceId, int commentId, CancellationToken ct = default)
    {
        ValidateIdentifiers(workspaceId, commentId);

        var comment = await this.dbContext.TicketComments
            .FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == commentId, ct);

        if (comment != null)
        {
            this.dbContext.TicketComments.Remove(comment);
            await this.dbContext.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Validates that workspace and entity IDs are positive values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if either ID is not positive</exception>
    private static void ValidateIdentifiers(int workspaceId, int entityId)
    {
        if (workspaceId <= 0)
        {
            throw new InvalidOperationException("Invalid workspace ID");
        }

        if (entityId <= 0)
        {
            throw new InvalidOperationException("Invalid entity ID");
        }
    }
}
