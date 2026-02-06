namespace Tickflo.Core.Services.Locations;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of setting up and configuring locations.
/// </summary>

/// <summary>
/// Handles location setup and configuration workflows.
/// </summary>
public interface ILocationSetupService
{
    /// <summary>
    /// Creates a new location in the workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">Location creation details</param>
    /// <param name="createdByUserId">User creating the location</param>
    /// <returns>The created location</returns>
    public Task<Location> CreateLocationAsync(int workspaceId, LocationCreationRequest request, int createdByUserId);

    /// <summary>
    /// Updates location details.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to update</param>
    /// <param name="request">Update details</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated location</returns>
    public Task<Location> UpdateLocationDetailsAsync(int workspaceId, int locationId, LocationUpdateRequest request, int updatedByUserId);

    /// <summary>
    /// Activates a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to activate</param>
    /// <param name="activatedByUserId">User performing activation</param>
    /// <returns>The activated location</returns>
    public Task<Location> ActivateLocationAsync(int workspaceId, int locationId, int activatedByUserId);

    /// <summary>
    /// Deactivates a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to deactivate</param>
    /// <param name="deactivatedByUserId">User performing deactivation</param>
    /// <returns>The deactivated location</returns>
    public Task<Location> DeactivateLocationAsync(int workspaceId, int locationId, int deactivatedByUserId);

    /// <summary>
    /// Assigns contacts to a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to assign contacts to</param>
    /// <param name="contactIds">Contact IDs to assign</param>
    /// <param name="assignedByUserId">User performing assignment</param>
    public Task AssignContactsToLocationAsync(int workspaceId, int locationId, List<int> contactIds, int assignedByUserId);

    /// <summary>
    /// Removes a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to remove</param>
    public Task RemoveLocationAsync(int workspaceId, int locationId);
}

public class LocationSetupService(TickfloDbContext dbContext) : ILocationSetupService
{
    private readonly TickfloDbContext dbContext = dbContext;

    /// <summary>
    /// Creates a new location with validation.
    /// </summary>
    public async Task<Location> CreateLocationAsync(
        int workspaceId,
        LocationCreationRequest request,
        int createdByUserId)
    {
        // Business rule: Location name must be unique within workspace
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Location name is required");
        }

        var name = request.Name.Trim();

        var nameLower = name.ToLower();
        var exists = await this.dbContext.Locations
            .AnyAsync(l => l.WorkspaceId == workspaceId && l.Name.ToLower() == nameLower);

        if (exists)
        {
            throw new InvalidOperationException($"Location '{name}' already exists in this workspace");
        }

        var location = new Location
        {
            WorkspaceId = workspaceId,
            Name = name,
            Address = string.IsNullOrWhiteSpace(request.Address) ? "" : request.Address.Trim(),
            Active = true // Business rule: New locations are active by default
        };

        this.dbContext.Locations.Add(location);
        await this.dbContext.SaveChangesAsync();

        return location;
    }

    /// <summary>
    /// Updates location details.
    /// </summary>
    public async Task<Location> UpdateLocationDetailsAsync(
        int workspaceId,
        int locationId,
        LocationUpdateRequest request,
        int updatedByUserId)
    {
        var location = await this.dbContext.Locations
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId)
            ?? throw new InvalidOperationException("Location not found");

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();

            // Check uniqueness if name is changing
            if (!string.Equals(location.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                var nameLower = name.ToLower();
                var exists = await this.dbContext.Locations
                    .AnyAsync(l => l.WorkspaceId == workspaceId && l.Id != locationId && l.Name.ToLower() == nameLower);

                if (exists)
                {
                    throw new InvalidOperationException($"Location '{name}' already exists in this workspace");
                }
            }

            location.Name = name;
        }

        if (request.Address != null)
        {
            location.Address = string.IsNullOrWhiteSpace(request.Address) ? "" : request.Address.Trim();
        }

        await this.dbContext.SaveChangesAsync();

        return location;
    }

    /// <summary>
    /// Activates a location.
    /// </summary>
    public async Task<Location> ActivateLocationAsync(int workspaceId, int locationId, int activatedByUserId)
    {
        var location = await this.dbContext.Locations
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId)
            ?? throw new InvalidOperationException("Location not found");

        if (location.Active)
        {
            return location; // Already active, no change needed
        }

        location.Active = true;

        await this.dbContext.SaveChangesAsync();

        // Could add: Notify users, log activation, etc.

        return location;
    }

    /// <summary>
    /// Deactivates a location.
    /// </summary>
    public async Task<Location> DeactivateLocationAsync(int workspaceId, int locationId, int deactivatedByUserId)
    {
        var location = await this.dbContext.Locations
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId)
            ?? throw new InvalidOperationException("Location not found");

        if (!location.Active)
        {
            return location; // Already inactive
        }

        // Business rule: Could check for active tickets or inventory at this location

        location.Active = false;

        await this.dbContext.SaveChangesAsync();

        // Could add: Reassign inventory, notify users, etc.

        return location;
    }

    /// <summary>
    /// Assigns contacts to a location.
    /// </summary>
    public async Task AssignContactsToLocationAsync(
        int workspaceId,
        int locationId,
        List<int> contactIds,
        int assignedByUserId)
    {
        var location = await this.dbContext.Locations
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId)
            ?? throw new InvalidOperationException("Location not found");

        // Business rule: Validate all contacts exist in the workspace
        if (contactIds.Count != 0)
        {
            var validContactIds = await this.dbContext.Contacts
                .Where(c => c.WorkspaceId == workspaceId)
                .Select(c => c.Id)
                .ToListAsync();

            var invalidContacts = contactIds.Except(validContactIds).ToList();

            if (invalidContacts.Count != 0)
            {
                throw new InvalidOperationException($"Invalid contact IDs: {string.Join(", ", invalidContacts)}");
            }
        }

        // TODO: Implement contact assignment logic when schema supports it
        // This might involve a location_contacts join table

        await this.dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a location.
    /// </summary>
    public async Task RemoveLocationAsync(int workspaceId, int locationId)
    {
        var location = await this.dbContext.Locations
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId)
            ?? throw new InvalidOperationException("Location not found");

        // Business rule: Could prevent deletion if location has inventory or tickets

        this.dbContext.Locations.Remove(location);
        await this.dbContext.SaveChangesAsync();
    }
}

/// <summary>
/// Request to create a new location.
/// </summary>
public class LocationCreationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

/// <summary>
/// Request to update location details.
/// </summary>
public class LocationUpdateRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}
