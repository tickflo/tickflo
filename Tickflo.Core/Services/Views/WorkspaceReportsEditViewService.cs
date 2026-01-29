namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Workspace;

public class WorkspaceReportsEditViewData
{
    public bool CanViewReports { get; set; }
    public bool CanEditReports { get; set; }
    public bool CanCreateReports { get; set; }
    public Report? ExistingReport { get; set; }
    public IReadOnlyDictionary<string, string[]> Sources { get; set; } = new Dictionary<string, string[]>();
}

public interface IWorkspaceReportsEditViewService
{
    public Task<WorkspaceReportsEditViewData> BuildAsync(int workspaceId, int userId, int reportId = 0);
}


public class WorkspaceReportsEditViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService,
    IReportingService reportingService) : IWorkspaceReportsEditViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IReportingService reportingService = reportingService;

    public async Task<WorkspaceReportsEditViewData> BuildAsync(int workspaceId, int userId, int reportId = 0)
    {
        var data = new WorkspaceReportsEditViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewReports = data.CanEditReports = data.CanCreateReports = true;
        }
        else if (permissions.TryGetValue("reports", out var rp))
        {
            data.CanViewReports = rp.CanView;
            data.CanEditReports = rp.CanEdit;
            data.CanCreateReports = rp.CanCreate;
        }

        data.Sources = this.reportingService.GetAvailableSources();

        if (reportId > 0)
        {
            data.ExistingReport = await this.dbContext.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId);
        }

        return data;
    }
}



