namespace Tickflo.Core.Services.Teams;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface ITeamListingService
{
    /// <summary>
    /// Gets teams for a workspace with member counts.
    /// </summary>
    public Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId);
}


public class TeamListingService(TickfloDbContext dbContext) : ITeamListingService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId)
    {
        var teams = await this.dbContext.Teams
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync();

        var memberCounts = new Dictionary<int, int>();

        foreach (var team in teams)
        {
            var memberCount = await this.dbContext.TeamMembers
                .Where(tm => tm.TeamId == team.Id)
                .CountAsync();
            memberCounts[team.Id] = memberCount;
        }

        return (teams.AsReadOnly(), memberCounts.AsReadOnly());
    }
}


