namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Workspace;

public record DashboardActivityPoint(string Label, int Created, int Closed);
public record DashboardMemberStat(int UserId, string Name, int ResolvedCount);
public record DashboardTicketListItem(int Id, string Subject, string Type, string Status, string StatusColor, string TypeColor, int? AssignedUserId, string? AssigneeName, DateTime UpdatedAt);

public record WorkspaceDashboardView(
    int TotalTickets,
    int OpenTickets,
    int ResolvedTickets,
    int ActiveMembers,
    IReadOnlyList<TicketStatus> StatusList,
    IReadOnlyList<TicketType> TypeList,
    IReadOnlyList<TicketPriority> PriorityList,
    IReadOnlyDictionary<string, int> PriorityCounts,
    string PrimaryColor,
    bool PrimaryIsHex,
    string SuccessColor,
    bool SuccessIsHex,
    IReadOnlyList<User> WorkspaceMembers,
    IReadOnlyList<Team> WorkspaceTeams,
    IReadOnlyList<DashboardActivityPoint> ActivitySeries,
    IReadOnlyList<DashboardMemberStat> TopMembers,
    string AvgResolutionLabel,
    IReadOnlyList<DashboardTicketListItem> RecentTickets,
    bool CanViewDashboard,
    bool CanViewTickets,
    string TicketViewScope);

public interface IWorkspaceDashboardViewService
{
    public Task<WorkspaceDashboardView> BuildAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        int rangeDays,
        string assignmentFilter);
}


public class WorkspaceDashboardViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService,
    IDashboardService dashboardService) : IWorkspaceDashboardViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IDashboardService dashboardService = dashboardService;

    public async Task<WorkspaceDashboardView> BuildAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        int rangeDays,
        string assignmentFilter)
    {
        var stats = await this.dashboardService.GetTicketStatsAsync(workspaceId, userId, scope, [.. teamIds]);

        var statusList = await this.dbContext.TicketStatuses
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .ToListAsync();

        var typeList = await this.dbContext.TicketTypes
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync();

        var priorityList = await this.dbContext.TicketPriorities
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();

        var priorityCounts = await this.dashboardService.GetPriorityCountsAsync(workspaceId, userId, scope, [.. teamIds]);

        var (primaryColor, primaryIsHex, successColor, successIsHex) = ResolveColors(statusList);

        var acceptedUserIds = await this.dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(uw => uw.WorkspaceId == workspaceId && uw.Accepted)
            .Select(uw => uw.UserId)
            .Distinct()
            .ToListAsync();

        var members = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => acceptedUserIds.Contains(u.Id))
            .ToListAsync();

        var teams = await this.dbContext.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync();

        var activityData = await this.dashboardService.GetActivitySeriesAsync(workspaceId, userId, scope, [.. teamIds], rangeDays);
        var activitySeries = activityData.Select(a => new DashboardActivityPoint(a.Date, a.Created, a.Closed)).ToList();

        var topMembers = await this.dashboardService.GetTopMembersAsync(workspaceId, userId, scope, [.. teamIds], topN: 5);
        var topMemberStats = topMembers.Select(m => new DashboardMemberStat(m.UserId, m.Name, m.ClosedCount)).ToList();

        var avgResolutionLabel = await this.dashboardService.GetAverageResolutionTimeAsync(workspaceId, userId, scope, [.. teamIds]);

        var recentTickets = await this.GetRecentTicketsAsync(workspaceId, userId, scope, teamIds, assignmentFilter, statusList, typeList);

        // Compute permissions
        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var canViewDashboard = false;
        var canViewTickets = false;
        var ticketViewScope = scope;

        if (isAdmin)
        {
            canViewDashboard = true;
            canViewTickets = true;
            ticketViewScope = "all";
        }
        else
        {
            var eff = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
            if (eff.TryGetValue("dashboard", out var dp))
            {
                canViewDashboard = dp.CanView;
            }

            if (eff.TryGetValue("tickets", out var tp))
            {
                canViewTickets = tp.CanView;
            }

            ticketViewScope = await this.workspaceAccessService.GetTicketViewScopeAsync(workspaceId, userId, isAdmin);
        }

        return new WorkspaceDashboardView(
            stats.TotalTickets,
            stats.OpenTickets,
            stats.ResolvedTickets,
            stats.ActiveMembers,
            statusList,
            typeList,
            priorityList,
            priorityCounts,
            primaryColor,
            primaryIsHex,
            successColor,
            successIsHex,
            members,
            teams,
            activitySeries,
            topMemberStats,
            avgResolutionLabel,
            recentTickets,
            canViewDashboard,
            canViewTickets,
            ticketViewScope);
    }

    private async Task<List<DashboardTicketListItem>> GetRecentTicketsAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        string assignmentFilter,
        IReadOnlyList<TicketStatus> statusList,
        IReadOnlyList<TicketType> typeList)
    {
        var scopedTickets = await this.ApplyTicketScopeAsync(workspaceId, userId, scope, teamIds);
        var allTickets = this.dashboardService.FilterTicketsByAssignment(scopedTickets, assignmentFilter, userId);

        var statusColor = statusList.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Color, StringComparer.OrdinalIgnoreCase);
        var statusColorById = statusList.ToDictionary(s => s.Id, s => s.Color);
        var statusNameById = statusList.ToDictionary(s => s.Id, s => s.Name);
        var typeColor = typeList.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Color, StringComparer.OrdinalIgnoreCase);
        var typeColorById = typeList.ToDictionary(t => t.Id, t => t.Color);
        var typeNameById = typeList.ToDictionary(t => t.Id, t => t.Name);

        var recent = allTickets
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Take(8)
            .ToList();

        var assigneeIds = recent.Where(t => t.AssignedUserId.HasValue)
            .Select(t => t.AssignedUserId!.Value)
            .Distinct()
            .ToList();

        var assignees = await this.dbContext.Users
            .AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return [.. recent.Select(t => new DashboardTicketListItem(
            t.Id,
            t.Subject,
            t.TicketTypeId.HasValue && typeNameById.TryGetValue(t.TicketTypeId.Value, out var typeName)
                ? typeName
                : "Unknown",
            t.StatusId.HasValue && statusNameById.TryGetValue(t.StatusId.Value, out var statusName)
                ? statusName
                : "Unknown",
            t.StatusId.HasValue && statusColorById.TryGetValue(t.StatusId.Value, out var cById)
                ? cById
                : "neutral",
            t.TicketTypeId.HasValue && typeColorById.TryGetValue(t.TicketTypeId.Value, out var tcById)
                ? tcById
                : "neutral",
            t.AssignedUserId,
            t.AssignedUserId.HasValue && assignees.TryGetValue(t.AssignedUserId.Value, out var assigneeName) ? assigneeName : null,
            t.UpdatedAt ?? t.CreatedAt))];
    }

    private async Task<List<Ticket>> ApplyTicketScopeAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds)
    {
        var query = this.dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId);

        if (scope == "mine")
        {
            return await query.Where(t => t.AssignedUserId == userId).ToListAsync();
        }
        else if (scope == "team")
        {
            var teamIdSet = teamIds.ToHashSet();
            return await query.Where(t => t.AssignedTeamId.HasValue && teamIdSet.Contains(t.AssignedTeamId.Value)).ToListAsync();
        }

        return await query.ToListAsync();
    }

    private static (string PrimaryColor, bool PrimaryIsHex, string SuccessColor, bool SuccessIsHex) ResolveColors(IReadOnlyList<TicketStatus> statusList)
    {
        var openStatus = statusList.FirstOrDefault(s => !s.IsClosedState);
        var closedStatus = statusList.FirstOrDefault(s => s.IsClosedState);

        var primaryColor = "primary";
        var primaryIsHex = false;
        var successColor = "success";
        var successIsHex = false;

        if (openStatus != null && !string.IsNullOrWhiteSpace(openStatus.Color))
        {
            primaryColor = openStatus.Color;
            primaryIsHex = openStatus.Color.StartsWith('#');
        }

        if (closedStatus != null && !string.IsNullOrWhiteSpace(closedStatus.Color))
        {
            successColor = closedStatus.Color;
            successIsHex = closedStatus.Color.StartsWith('#');
        }

        return (primaryColor, primaryIsHex, successColor, successIsHex);
    }
}



