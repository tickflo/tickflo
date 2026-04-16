namespace Tickflo.Core.Entities;

public enum TicketHistoryAction
{
    Created,
    FieldChanged,
    Assigned,
    TeamAssigned,
    Unassigned,
    ReassignmentNote,
    Closed,
    Reopened,
    Resolved,
    Cancelled
}

public static class TicketHistoryActionExtensions
{
    public static string ToDatabaseValue(this TicketHistoryAction action) =>
        action switch
        {
            TicketHistoryAction.Created => "created",
            TicketHistoryAction.FieldChanged => "field_changed",
            TicketHistoryAction.Assigned => "assigned",
            TicketHistoryAction.TeamAssigned => "team_assigned",
            TicketHistoryAction.Unassigned => "unassigned",
            TicketHistoryAction.ReassignmentNote => "reassignment_note",
            TicketHistoryAction.Closed => "closed",
            TicketHistoryAction.Reopened => "reopened",
            TicketHistoryAction.Resolved => "resolved",
            TicketHistoryAction.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

    public static bool TryParseDatabaseValue(string? value, out TicketHistoryAction action)
    {
        switch (value)
        {
            case "created":
                action = TicketHistoryAction.Created;
                return true;
            case "field_changed":
                action = TicketHistoryAction.FieldChanged;
                return true;
            case "assigned":
                action = TicketHistoryAction.Assigned;
                return true;
            case "team_assigned":
                action = TicketHistoryAction.TeamAssigned;
                return true;
            case "unassigned":
                action = TicketHistoryAction.Unassigned;
                return true;
            case "reassignment_note":
                action = TicketHistoryAction.ReassignmentNote;
                return true;
            case "closed":
                action = TicketHistoryAction.Closed;
                return true;
            case "reopened":
                action = TicketHistoryAction.Reopened;
                return true;
            case "resolved":
                action = TicketHistoryAction.Resolved;
                return true;
            case "cancelled":
                action = TicketHistoryAction.Cancelled;
                return true;
            default:
                action = default;
                return false;
        }
    }
}
