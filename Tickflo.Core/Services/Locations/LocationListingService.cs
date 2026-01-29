namespace Tickflo.Core.Services.Locations;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using static Tickflo.Core.Services.Locations.ILocationListingService;
public interface ILocationListingService
{
    public record LocationItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int ContactCount { get; set; }
        public string ContactPreview { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gets enriched location items for a workspace with contact preview info.
    /// </summary>
    public Task<IReadOnlyList<LocationItem>> GetListAsync(int workspaceId);
}


public class LocationListingService(TickfloDbContext dbContext) : ILocationListingService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<IReadOnlyList<LocationItem>> GetListAsync(int workspaceId)
    {
        var list = await this.dbContext.Locations
            .Where(l => l.WorkspaceId == workspaceId)
            .ToListAsync();

        var items = new List<LocationItem>();

        foreach (var location in list)
        {
            var contactIds = await this.dbContext.ContactLocations
                .Where(cl => cl.WorkspaceId == workspaceId && cl.LocationId == location.Id)
                .Select(cl => cl.ContactId)
                .ToListAsync();

            var contactCount = contactIds.Count;

            var previewNames = await this.dbContext.Contacts
                .Where(c => c.WorkspaceId == workspaceId && contactIds.Take(3).Contains(c.Id))
                .Select(c => c.Name)
                .ToListAsync();

            var preview = string.Join(", ", previewNames);

            items.Add(new LocationItem
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Active = location.Active,
                ContactCount = contactCount,
                ContactPreview = preview
            });
        }

        return items.AsReadOnly();
    }
}



