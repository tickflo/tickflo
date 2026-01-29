namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Workspace;

public class WorkspaceSettingsViewData
{
    public bool CanViewSettings { get; set; }
    public bool CanEditSettings { get; set; }
    public bool CanCreateSettings { get; set; }

    public IReadOnlyList<Entities.TicketStatus> Statuses { get; set; } = [];
    public IReadOnlyList<Entities.TicketPriority> Priorities { get; set; } = [];
    public IReadOnlyList<Entities.TicketType> Types { get; set; } = [];

    public bool NotificationsEnabled { get; set; } = true;
    public bool EmailIntegrationEnabled { get; set; } = true;
    public string EmailProvider { get; set; } = "smtp";
    public bool SmsIntegrationEnabled { get; set; }
    public string SmsProvider { get; set; } = "none";
    public bool PushIntegrationEnabled { get; set; }
    public string PushProvider { get; set; } = "none";
    public bool InAppNotificationsEnabled { get; set; } = true;
    public int BatchNotificationDelay { get; set; } = 30;
    public int DailySummaryHour { get; set; } = 9;
    public bool MentionNotificationsUrgent { get; set; } = true;
    public bool TicketAssignmentNotificationsHigh { get; set; } = true;
}

public interface IWorkspaceSettingsViewService
{
    public Task<WorkspaceSettingsViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceSettingsViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceSettingsService workspaceSettingsService) : IWorkspaceSettingsViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IWorkspaceSettingsService workspaceSettingsService = workspaceSettingsService;

    public async Task<WorkspaceSettingsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceSettingsViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanViewSettings = data.CanEditSettings = data.CanCreateSettings = true;
        }
        else
        {
            var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
            if (permissions.TryGetValue("settings", out var eff))
            {
                data.CanViewSettings = eff.CanView;
                data.CanEditSettings = eff.CanEdit;
                data.CanCreateSettings = eff.CanCreate;
            }
        }

        // Ensure defaults and load lists
        await this.workspaceSettingsService.EnsureDefaultsExistAsync(workspaceId);

        data.Statuses = await this.dbContext.TicketStatuses
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .ToListAsync();

        data.Priorities = await this.dbContext.TicketPriorities
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();

        data.Types = await this.dbContext.TicketTypes
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync();

        // Notification defaults (placeholder until persisted storage exists)
        data.NotificationsEnabled = true;
        data.EmailIntegrationEnabled = true;
        data.EmailProvider = "smtp";
        data.SmsIntegrationEnabled = false;
        data.SmsProvider = "none";
        data.PushIntegrationEnabled = false;
        data.PushProvider = "none";
        data.InAppNotificationsEnabled = true;
        data.BatchNotificationDelay = 30;
        data.DailySummaryHour = 9;
        data.MentionNotificationsUrgent = true;
        data.TicketAssignmentNotificationsHigh = true;

        return data;
    }
}



