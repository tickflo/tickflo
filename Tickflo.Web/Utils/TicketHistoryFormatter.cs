namespace Tickflo.Web.Utils;

using Tickflo.Core.Entities;

public static class TicketHistoryFormatter
{
    private const string DefaultFieldName = "unknown field";
    private const string EmptyValueText = "(empty)";

    public static string FormatFieldName(TicketHistoryField? field) => field switch
    {
        TicketHistoryField.Subject => "subject",
        TicketHistoryField.Description => "description",
        TicketHistoryField.Type => "type",
        TicketHistoryField.Priority => "priority",
        TicketHistoryField.Status => "status",
        TicketHistoryField.Contact => "contact",
        TicketHistoryField.AssignedUser => "assignee",
        TicketHistoryField.AssignedTeam => "assigned team",
        TicketHistoryField.Location => "location",
        TicketHistoryField.Inventory => "inventory",
        TicketHistoryField.DueDate => "due date",
        _ => DefaultFieldName
    };

    public static string FormatActionDescription(TicketHistoryAction action, TicketHistoryField? field) => action switch
    {
        TicketHistoryAction.Created => "created this ticket",
        TicketHistoryAction.FieldChanged => $"changed {FormatFieldName(field)}",
        TicketHistoryAction.Assigned => "assigned this ticket",
        TicketHistoryAction.TeamAssigned => "assigned this ticket to a team",
        TicketHistoryAction.Unassigned => "removed the assignee",
        TicketHistoryAction.ReassignmentNote => "added a reassignment note",
        TicketHistoryAction.Closed => "closed this ticket",
        TicketHistoryAction.Reopened => "reopened this ticket",
        TicketHistoryAction.Resolved => "marked this ticket as resolved",
        TicketHistoryAction.Cancelled => "cancelled this ticket",
        _ => "performed an action"
    };

    public static string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? EmptyValueText : value;

    public static bool ShouldShowValueChange(TicketHistoryAction action) =>
        action == TicketHistoryAction.FieldChanged;
}

