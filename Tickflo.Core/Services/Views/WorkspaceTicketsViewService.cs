namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

/// <summary>
/// Implementation of tickets view service.
/// Aggregates tickets metadata and permissions for list display.
/// </summary>

/// <summary>
/// Service for aggregating and preparing ticket list view data.
/// Consolidates tickets, metadata, and permissions for display.
/// </summary>
public interface IWorkspaceTicketsViewService
{
    /// <summary>
    /// Builds aggregated view data for tickets page.
    /// </summary>
    /// <param name="workspaceId">The workspace to load tickets for</param>
    /// <param name="userId">Current user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>View data containing tickets, statuses, priorities, types, and permissions</returns>
    public Task<WorkspaceTicketsViewData> BuildAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tickets for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace to load tickets for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all tickets in the workspace</returns>
    public Task<IEnumerable<Ticket>> GetAllTicketsAsync(int workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single ticket by ID.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ticket if found, null otherwise</returns>
    public Task<Ticket?> GetTicketAsync(int workspaceId, int ticketId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated view data for tickets page.
/// </summary>
public class WorkspaceTicketsViewData
{
    /// <summary>
    /// Ticket statuses for the workspace.
    /// </summary>
    public IReadOnlyList<TicketStatus> Statuses { get; set; } = [];

    /// <summary>
    /// Map of status name to color.
    /// </summary>
    public Dictionary<string, string> StatusColorByName { get; set; } = [];

    /// <summary>
    /// Ticket priorities for the workspace.
    /// </summary>
    public IReadOnlyList<TicketPriority> Priorities { get; set; } = [];

    /// <summary>
    /// Map of priority name to color.
    /// </summary>
    public Dictionary<string, string> PriorityColorByName { get; set; } = [];

    /// <summary>
    /// Ticket types for the workspace.
    /// </summary>
    public IReadOnlyList<TicketType> Types { get; set; } = [];

    /// <summary>
    /// Map of type name to color.
    /// </summary>
    public Dictionary<string, string> TypeColorByName { get; set; } = [];

    /// <summary>
    /// Teams in the workspace, indexed by ID.
    /// </summary>
    public Dictionary<int, Team> TeamsById { get; set; } = [];

    /// <summary>
    /// Contacts in the workspace, indexed by ID.
    /// </summary>
    public Dictionary<int, Contact> ContactsById { get; set; } = [];

    /// <summary>
    /// Workspace members, indexed by user ID.
    /// </summary>
    public Dictionary<int, User> UsersById { get; set; } = [];

    /// <summary>
    /// Locations available in the workspace.
    /// </summary>
    public List<Location> LocationOptions { get; set; } = [];

    /// <summary>
    /// Locations, indexed by ID.
    /// </summary>
    public Dictionary<int, Location> LocationsById { get; set; } = [];

    /// <summary>
    /// Whether user can create tickets.
    /// </summary>
    public bool CanCreateTickets { get; set; }

    /// <summary>
    /// Whether user can edit tickets.
    /// </summary>
    public bool CanEditTickets { get; set; }

    /// <summary>
    /// Ticket view scope for the user ("all", "mine", or "team").
    /// </summary>
    public string TicketViewScope { get; set; } = "all";

    /// <summary>
    /// Team IDs the user belongs to (when scope is "team").
    /// </summary>
    public List<int> UserTeamIds { get; set; } = [];
}

public class WorkspaceTicketsViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceTicketsViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceTicketsViewData> BuildAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Determine if user is admin
        var isAdmin = userId > 0 && await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var data = new WorkspaceTicketsViewData();

        // Load statuses with fallback defaults
        var statuses = await this.dbContext.TicketStatuses
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        var statusList = statuses.Count > 0
            ? statuses
            :
            [
                new() { WorkspaceId = workspaceId, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new() { WorkspaceId = workspaceId, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new() { WorkspaceId = workspaceId, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true },
            ];
        data.Statuses = statusList;
        data.StatusColorByName = statusList
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load priorities with fallback defaults
        var priorities = await this.dbContext.TicketPriorities
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        var priorityList = priorities.Count > 0
            ? priorities
            :
            [
                new() { WorkspaceId = workspaceId, Name = "Low", Color = "warning", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "High", Color = "error", SortOrder = 3 },
            ];
        data.Priorities = priorityList;
        data.PriorityColorByName = priorityList
            .GroupBy(p => p.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load types with fallback defaults
        var types = await this.dbContext.TicketTypes
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        var typeList = types.Count > 0
            ? types
            :
            [
                new() { WorkspaceId = workspaceId, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Bug", Color = "error", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "Feature", Color = "primary", SortOrder = 3 },
            ];
        data.Types = typeList;
        data.TypeColorByName = typeList
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load teams
        var teams = await this.dbContext.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        data.TeamsById = teams.ToDictionary(t => t.Id, t => t);

        // Load contacts
        var contacts = await this.dbContext.Contacts
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        data.ContactsById = contacts.ToDictionary(c => c.Id, c => c);

        // Load workspace members (batch load users)
        var memberships = await this.dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(uw => uw.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var users = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        data.UsersById = users.ToDictionary(u => u.Id, u => u);

        // Load locations
        var locations = await this.dbContext.Locations
            .AsNoTracking()
            .Where(l => l.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        data.LocationOptions = [.. locations];
        data.LocationsById = locations.ToDictionary(l => l.Id, l => l);

        // Determine permissions
        if (userId > 0)
        {
            var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
            if (permissions.TryGetValue("tickets", out var ticketPerms))
            {
                data.CanCreateTickets = ticketPerms.CanCreate;
                data.CanEditTickets = ticketPerms.CanEdit;
            }
            else
            {
                data.CanCreateTickets = isAdmin;
                data.CanEditTickets = isAdmin;
            }
        }

        // Determine scope and team IDs
        var scope = await this.workspaceAccessService.GetTicketViewScopeAsync(workspaceId, userId, isAdmin);
        data.TicketViewScope = scope;
        if (scope == "team" && userId > 0)
        {
            var userTeamIds = await this.dbContext.TeamMembers
                .AsNoTracking()
                .Where(tm => tm.UserId == userId)
                .Join(
                    this.dbContext.Teams,
                    tm => tm.TeamId,
                    t => t.Id,
                    (tm, t) => new { tm, t })
                .Where(x => x.t.WorkspaceId == workspaceId)
                .Select(x => x.t.Id)
                .ToListAsync(cancellationToken);
            data.UserTeamIds = [.. userTeamIds];
        }

        return data;
    }

    public async Task<IEnumerable<Ticket>> GetAllTicketsAsync(int workspaceId, CancellationToken cancellationToken = default) =>
        await this.dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

    public async Task<Ticket?> GetTicketAsync(int workspaceId, int ticketId, CancellationToken cancellationToken = default) =>
        await this.dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == ticketId, cancellationToken);
}


