namespace Tickflo.Core.Entities;

public class TicketHistory
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int TicketId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TicketHistoryAction Action { get; set; }
    public string? Field { get; set; } // e.g., Subject, Description, Priority, Status, Type, AssignedUserId, InventoryRef, ContactId
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Note { get; set; }
}
