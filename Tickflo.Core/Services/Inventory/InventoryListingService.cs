namespace Tickflo.Core.Services.Inventory;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
public interface IInventoryListingService
{
    /// <summary>
    /// Gets inventory items for a workspace with optional filtering.
    /// </summary>
    public Task<IReadOnlyList<Entities.Inventory>> GetListAsync(
        int workspaceId,
        string? searchQuery = null,
        string? statusFilter = null);
}


public class InventoryListingService(TickfloDbContext dbContext) : IInventoryListingService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<IReadOnlyList<Entities.Inventory>> GetListAsync(
        int workspaceId,
        string? searchQuery = null,
        string? statusFilter = null)
    {
        var query = this.dbContext.Inventory
            .Where(i => i.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var trimmedQuery = searchQuery.Trim();
            query = query.Where(i =>
                i.Sku.Contains(trimmedQuery) ||
                i.Name.Contains(trimmedQuery));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(i => i.Status == statusFilter);
        }

        var result = await query.ToListAsync();
        return result.AsReadOnly();
    }
}



