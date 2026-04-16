namespace Tickflo.Web.Utils;

using Tickflo.Core.Entities;

public static class TicketHistoryFormatter
{
    private const string FieldSubject = "Subject";
    private const string FieldDescription = "Description";
    private const string FieldType = "Type";
    private const string FieldPriority = "Priority";
    private const string FieldStatus = "Status";
    private const string FieldContactId = "ContactId";
    private const string FieldAssignedUserId = "AssignedUserId";
    private const string FieldAssignedTeamId = "AssignedTeamId";
    private const string FieldLocationId = "LocationId";
    private const string FieldInventory = "Inventory";
    private const string FieldDueDate = "DueDate";

    private const string DefaultFieldName = "unknown field";
    private const string DefaultActionText = "performed an action";
    private const string EmptyValueText = "(empty)";

    public static string FormatFieldName(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return DefaultFieldName;
        }

        return field switch
        {
            FieldSubject => "subject",
            FieldDescription => "description",
            FieldType => "type",
            FieldPriority => "priority",
            FieldStatus => "status",
            FieldContactId => "contact",
            FieldAssignedUserId => "assignee",
            FieldAssignedTeamId => "assigned team",
            FieldLocationId => "location",
            FieldInventory => "inventory",
            FieldDueDate => "due date",
            _ => field.ToLower(System.Globalization.CultureInfo.CurrentCulture)
        };
    }

    public static string FormatActionDescription(string? action, string? field, string? note)
    {
        if (!TicketHistoryActionExtensions.TryParseDatabaseValue(action, out var ticketHistoryAction))
        {
            return DefaultActionText;
        }

        return ticketHistoryAction switch
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
            _ => FormatUnknownAction(action!, note)
        };
    }

    private static string FormatUnknownAction(string action, string? note)
    {
        var actionText = action.Replace("_", " ");
        return string.IsNullOrEmpty(note) ? actionText : $"{actionText} {note}".Trim();
    }

    public static string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? EmptyValueText : value;

    public static bool ShouldShowValueChange(string? action) =>
        TicketHistoryActionExtensions.TryParseDatabaseValue(action, out var ticketHistoryAction) &&
        ticketHistoryAction == TicketHistoryAction.FieldChanged;
}
