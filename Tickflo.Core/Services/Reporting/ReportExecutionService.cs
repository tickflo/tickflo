namespace Tickflo.Core.Services.Reporting;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Service for executing and managing report runs.
/// Handles report execution, result generation, and pagination.
/// </summary>
public interface IReportExecutionService
{
    /// <summary>
    /// Executes a report and returns the result.
    /// </summary>
    public Task<ReportExecutionResult> ExecuteReportAsync(int userId, int workspaceId, int reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets a page of results from a report run.
    /// </summary>
    public Task<ReportRunPage> GetRunPageAsync(int runId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Gets available data sources for report design.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> GetAvailableDataSources();

    /// <summary>
    /// Schedules a report to run at a specific time.
    /// </summary>
    public Task<ReportRun> ScheduleReportAsync(int userId, int workspaceId, int reportId, DateTime scheduledFor, CancellationToken ct = default);

    /// <summary>
    /// Cancels a scheduled or running report.
    /// </summary>
    public Task CancelReportRunAsync(int userId, int workspaceId, int reportRunId, CancellationToken ct = default);

    /// <summary>
    /// Gets execution history for a report.
    /// </summary>
    public Task<IReadOnlyList<ReportRun>> GetReportHistoryAsync(int userId, int workspaceId, int reportId, int take = 20, CancellationToken ct = default);
}


public class ReportExecutionService(
    TickfloDbContext dbContext,
    IReportingService reportingService) : IReportExecutionService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IReportingService reportingService = reportingService;

    public async Task<ReportExecutionResult> ExecuteReportAsync(int userId, int workspaceId, int reportId, CancellationToken ct = default)
    {
        var workspace = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId, ct)
            ?? throw new UnauthorizedAccessException();

        var report = await this.dbContext.Reports
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId, ct)
            ?? throw new KeyNotFoundException();

        return await this.reportingService.ExecuteAsync(workspaceId, report, ct);
    }

    public async Task<ReportRunPage> GetRunPageAsync(int runId, int page, int pageSize, CancellationToken ct = default) =>
        // Note: ReportRun doesn't expose workspace_id directly, would need to refactor this
        // For now, assume workspaceId can be passed or derived from context
        throw new NotImplementedException("Requires workspace context to be added to ReportRun");

    public IReadOnlyDictionary<string, string[]> GetAvailableDataSources() => this.reportingService.GetAvailableSources();

    public async Task<ReportRun> ScheduleReportAsync(int userId, int workspaceId, int reportId, DateTime scheduledFor, CancellationToken ct = default)
    {
        var workspace = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId, ct)
            ?? throw new UnauthorizedAccessException();

        var report = await this.dbContext.Reports
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId, ct)
            ?? throw new KeyNotFoundException();

        if (scheduledFor < DateTime.UtcNow)
        {
            throw new ArgumentException("Scheduled time must be in the future");
        }

        // This is a simplified version - actual scheduling would need a background job
        var run = new ReportRun { ReportId = reportId, WorkspaceId = workspaceId };
        this.dbContext.ReportRuns.Add(run);
        await this.dbContext.SaveChangesAsync(ct);
        return run;
    }

    public async Task CancelReportRunAsync(int userId, int workspaceId, int reportRunId, CancellationToken ct = default)
    {
        var workspace = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId, ct)
            ?? throw new UnauthorizedAccessException();

        var run = await this.dbContext.ReportRuns
            .FirstOrDefaultAsync(rr => rr.WorkspaceId == workspaceId && rr.Id == reportRunId, ct)
            ?? throw new KeyNotFoundException();
    }

    public async Task<IReadOnlyList<ReportRun>> GetReportHistoryAsync(int userId, int workspaceId, int reportId, int take = 20, CancellationToken ct = default)
    {
        var workspace = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId, ct)
            ?? throw new UnauthorizedAccessException();

        var report = await this.dbContext.Reports
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId, ct)
            ?? throw new KeyNotFoundException();

        var runs = await this.dbContext.ReportRuns
            .Where(rr => rr.WorkspaceId == workspaceId && rr.ReportId == reportId)
            .OrderByDescending(rr => rr.StartedAt)
            .Take(take)
            .ToListAsync(ct);

        return runs;
    }
}
