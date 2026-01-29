namespace Tickflo.Core.Data;

/// <summary>
/// Represents aggregated permissions for a section across all user's roles.
/// Computed by applying OR logic to all role permissions in the section.
/// </summary>
#pragma warning disable CA1711
public class EffectiveSectionPermission
#pragma warning restore CA1711
{
    public string Section { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string? TicketViewScope { get; set; }
}
