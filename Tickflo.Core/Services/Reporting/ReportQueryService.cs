namespace Tickflo.Core.Services.Reporting;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
public record ReportListItem(int Id, string Name, bool Ready, DateTime? LastRun);

public interface IReportQueryService
{
    public Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default);
}


public class ReportQueryService(TickfloDbContext dbContext) : IReportQueryService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default)
    {
        var list = await this.dbContext.Reports
            .Where(r => r.WorkspaceId == workspaceId)
            .ToListAsync(ct);

        return [.. list.Select(r => new ReportListItem(r.Id, r.Name, r.Ready, r.LastRun))];
    }
}


