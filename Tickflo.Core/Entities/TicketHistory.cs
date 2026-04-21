namespace Tickflo.Core.Entities;

public class TicketHistory
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int TicketId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TicketHistoryAction Action { get; set; }
    public TicketHistoryField? Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Note { get; set; }
}
