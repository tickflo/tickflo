namespace Tickflo.Core.Entities;

public enum TicketHistoryAction
{
    Created = 1,
    FieldChanged = 2,
    Assigned = 3,
    TeamAssigned = 4,
    Unassigned = 5,
    ReassignmentNote = 6,
    Closed = 7,
    Reopened = 8,
    Resolved = 9,
    Cancelled = 10
}
