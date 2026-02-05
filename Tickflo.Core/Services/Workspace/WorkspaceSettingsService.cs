namespace Tickflo.Core.Services.Workspace;

using System.Text;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
/// <summary>
/// Service for managing workspace settings including status, priority, and type configurations.
/// </summary>
using WorkspaceEntity = Entities.Workspace;

/// <summary>
/// Service for managing workspace settings including status, priority, and type configurations.
/// </summary>
public interface IWorkspaceSettingsService
{
    /// <summary>
    /// Validates and updates workspace basic settings (name, slug).
    /// </summary>
    /// <param name="workspaceId">Workspace to update</param>
    /// <param name="name">New name</param>
    /// <param name="slug">New slug</param>
    /// <returns>Updated workspace</returns>
    /// <exception cref="InvalidOperationException">If slug is already in use</exception>
    public Task<WorkspaceEntity> UpdateWorkspaceBasicSettingsAsync(int workspaceId, string name, string slug);

    /// <summary>
    /// Bootstraps default status/priority/type if none exist for a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace to bootstrap</param>
    public Task EnsureDefaultsExistAsync(int workspaceId);

    /// <summary>
    /// Adds a new ticket status.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Status name</param>
    /// <param name="color">Color theme</param>
    /// <param name="isClosedState">Whether this represents a closed state</param>
    /// <returns>Created status</returns>
    public Task<TicketStatus> AddStatusAsync(int workspaceId, string name, string color, bool isClosedState = false);

    /// <summary>
    /// Updates an existing ticket status.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="statusId">Status to update</param>
    /// <param name="name">New name</param>
    /// <param name="color">New color</param>
    /// <param name="sortOrder">New sort order</param>
    /// <param name="isClosedState">New closed state flag</param>
    /// <returns>Updated status</returns>
    public Task<TicketStatus> UpdateStatusAsync(
        int workspaceId,
        int statusId,
        string name,
        string color,
        int sortOrder,
        bool isClosedState);

    /// <summary>
    /// Deletes a ticket status.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="statusId">Status to delete</param>
    public Task DeleteStatusAsync(int workspaceId, int statusId);

    /// <summary>
    /// Adds a new ticket priority.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Priority name</param>
    /// <param name="color">Color theme</param>
    /// <returns>Created priority</returns>
    public Task<TicketPriority> AddPriorityAsync(int workspaceId, string name, string color);

    /// <summary>
    /// Updates an existing ticket priority.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="priorityId">Priority to update</param>
    /// <param name="name">New name</param>
    /// <param name="color">New color</param>
    /// <param name="sortOrder">New sort order</param>
    /// <returns>Updated priority</returns>
    public Task<TicketPriority> UpdatePriorityAsync(
        int workspaceId,
        int priorityId,
        string name,
        string color,
        int sortOrder);

    /// <summary>
    /// Deletes a ticket priority.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="priorityId">Priority to delete</param>
    public Task DeletePriorityAsync(int workspaceId, int priorityId);

    /// <summary>
    /// Adds a new ticket type.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Type name</param>
    /// <param name="color">Color theme</param>
    /// <returns>Created type</returns>
    public Task<TicketType> AddTypeAsync(int workspaceId, string name, string color);

    /// <summary>
    /// Updates an existing ticket type.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="typeId">Type to update</param>
    /// <param name="name">New name</param>
    /// <param name="color">New color</param>
    /// <param name="sortOrder">New sort order</param>
    /// <returns>Updated type</returns>
    public Task<TicketType> UpdateTypeAsync(
        int workspaceId,
        int typeId,
        string name,
        string color,
        int sortOrder);

    /// <summary>
    /// Deletes a ticket type.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="typeId">Type to delete</param>
    public Task DeleteTypeAsync(int workspaceId, int typeId);

    /// <summary>
    /// Performs a bulk update of workspace settings including workspace basic settings,
    /// statuses, priorities, and types based on the provided request.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">The bulk update request containing all changes to apply</param>
    /// <returns>Result containing updated workspace and change count</returns>
    public Task<BulkSettingsUpdateResult> BulkUpdateSettingsAsync(int workspaceId, BulkSettingsUpdateRequest request);
}

public class WorkspaceSettingsService(TickfloDbContext dbContext) : IWorkspaceSettingsService
{
    #region Constants
    private const string WorkspaceNotFoundError = "Workspace not found";
    private const string SlugInUseError = "Slug is already in use";
    private static readonly CompositeFormat NameRequiredError = CompositeFormat.Parse("{0} name is required");
    private static readonly CompositeFormat AlreadyExistsError = CompositeFormat.Parse("{0} '{1}' already exists");
    private static readonly CompositeFormat NotFoundError = CompositeFormat.Parse("{0} not found");
    private const string DefaultColor = "neutral";
    #endregion

    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<WorkspaceEntity> UpdateWorkspaceBasicSettingsAsync(int workspaceId, string name, string slug)
    {
        var workspace = await this.GetWorkspaceOrThrowAsync(workspaceId);
        workspace.Name = name.Trim();

        var newSlug = slug.Trim();
        if (newSlug != workspace.Slug)
        {
            await this.ValidateSlugIsAvailableAsync(newSlug, workspaceId);
            workspace.Slug = newSlug;
        }

        workspace.UpdatedAt = DateTime.UtcNow;
        await this.dbContext.SaveChangesAsync();
        return workspace;
    }

    public async Task EnsureDefaultsExistAsync(int workspaceId)
    {
        // Bootstrap statuses
        var statusCount = await this.dbContext.TicketStatuses
            .CountAsync(s => s.WorkspaceId == workspaceId);

        if (statusCount == 0)
        {
            var defaults = new[]
            {
                new TicketStatus { WorkspaceId = workspaceId, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new TicketStatus { WorkspaceId = workspaceId, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new TicketStatus { WorkspaceId = workspaceId, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true }
            };

            this.dbContext.TicketStatuses.AddRange(defaults);
            await this.dbContext.SaveChangesAsync();
        }

        // Bootstrap priorities
        var priorityCount = await this.dbContext.TicketPriorities
            .CountAsync(p => p.WorkspaceId == workspaceId);

        if (priorityCount == 0)
        {
            var defaults = new[]
            {
                new TicketPriority { WorkspaceId = workspaceId, Name = "Low", Color = "warning", SortOrder = 1 },
                new TicketPriority { WorkspaceId = workspaceId, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new TicketPriority { WorkspaceId = workspaceId, Name = "High", Color = "error", SortOrder = 3 }
            };

            this.dbContext.TicketPriorities.AddRange(defaults);
            await this.dbContext.SaveChangesAsync();
        }

        // Bootstrap types
        var typeCount = await this.dbContext.TicketTypes
            .CountAsync(t => t.WorkspaceId == workspaceId);

        if (typeCount == 0)
        {
            var defaults = new[]
            {
                new TicketType { WorkspaceId = workspaceId, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new TicketType { WorkspaceId = workspaceId, Name = "Bug", Color = "error", SortOrder = 2 },
                new TicketType { WorkspaceId = workspaceId, Name = "Feature", Color = "primary", SortOrder = 3 }
            };

            this.dbContext.TicketTypes.AddRange(defaults);
            await this.dbContext.SaveChangesAsync();
        }
    }

    public async Task<TicketStatus> AddStatusAsync(int workspaceId, string name, string color, bool isClosedState = false)
    {
        var trimmedName = ValidateAndTrimName(name, "Status");
        var trimmedColor = TrimColorOrDefault(color);

        var trimmedNameLower = trimmedName.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        var exists = await this.dbContext.TicketStatuses
            .AnyAsync(s => s.WorkspaceId == workspaceId && s.Name.Equals(trimmedNameLower, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException(string.Format(null, AlreadyExistsError, "Status", trimmedName));
        }

        var maxOrder = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId)
            .MaxAsync(s => (int?)s.SortOrder) ?? 0;

        var status = new TicketStatus
        {
            WorkspaceId = workspaceId,
            Name = trimmedName,
            Color = trimmedColor,
            SortOrder = maxOrder + 1,
            IsClosedState = isClosedState
        };

        this.dbContext.TicketStatuses.Add(status);
        await this.dbContext.SaveChangesAsync();
        return status;
    }

    public async Task<TicketStatus> UpdateStatusAsync(
        int workspaceId,
        int statusId,
        string name,
        string color,
        int sortOrder,
        bool isClosedState)
    {
        var status = await this.dbContext.TicketStatuses
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == statusId)
            ?? throw new InvalidOperationException(string.Format(null, NotFoundError, "Status"));

        status.Name = ValidateAndTrimName(name, "Status");
        status.Color = TrimColorOrDefault(color);
        status.SortOrder = sortOrder;
        status.IsClosedState = isClosedState;

        await this.dbContext.SaveChangesAsync();
        return status;
    }

    public async Task DeleteStatusAsync(int workspaceId, int statusId)
    {
        var status = await this.dbContext.TicketStatuses
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == statusId);

        if (status != null)
        {
            this.dbContext.TicketStatuses.Remove(status);
            await this.dbContext.SaveChangesAsync();
        }
    }

    public async Task<TicketPriority> AddPriorityAsync(int workspaceId, string name, string color)
    {
        var trimmedName = ValidateAndTrimName(name, "Priority");
        var trimmedColor = TrimColorOrDefault(color);

        var trimmedNameLower = trimmedName.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        var exists = await this.dbContext.TicketPriorities
            .AnyAsync(p => p.WorkspaceId == workspaceId && p.Name.Equals(trimmedNameLower, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException(string.Format(null, AlreadyExistsError, "Priority", trimmedName));
        }

        var maxOrder = await this.dbContext.TicketPriorities
            .Where(p => p.WorkspaceId == workspaceId)
            .MaxAsync(p => (int?)p.SortOrder) ?? 0;

        var priority = new TicketPriority
        {
            WorkspaceId = workspaceId,
            Name = trimmedName,
            Color = trimmedColor,
            SortOrder = maxOrder + 1
        };

        this.dbContext.TicketPriorities.Add(priority);
        await this.dbContext.SaveChangesAsync();
        return priority;
    }

    public async Task<TicketPriority> UpdatePriorityAsync(
        int workspaceId,
        int priorityId,
        string name,
        string color,
        int sortOrder)
    {
        var priority = await this.dbContext.TicketPriorities
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.Id == priorityId)
            ?? throw new InvalidOperationException(string.Format(null, NotFoundError, "Priority"));

        priority.Name = ValidateAndTrimName(name, "Priority");
        priority.Color = TrimColorOrDefault(color);
        priority.SortOrder = sortOrder;

        await this.dbContext.SaveChangesAsync();
        return priority;
    }

    public async Task DeletePriorityAsync(int workspaceId, int priorityId)
    {
        var priority = await this.dbContext.TicketPriorities
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.Id == priorityId);

        if (priority != null)
        {
            this.dbContext.TicketPriorities.Remove(priority);
            await this.dbContext.SaveChangesAsync();
        }
    }

    public async Task<TicketType> AddTypeAsync(int workspaceId, string name, string color)
    {
        var trimmedName = ValidateAndTrimName(name, "Type");
        var trimmedColor = TrimColorOrDefault(color);

        var trimmedNameLower = trimmedName.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        var exists = await this.dbContext.TicketTypes
            .AnyAsync(t => t.WorkspaceId == workspaceId && t.Name.Equals(trimmedNameLower, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException(string.Format(null, AlreadyExistsError, "Type", trimmedName));
        }

        var maxOrder = await this.dbContext.TicketTypes
            .Where(t => t.WorkspaceId == workspaceId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        var type = new TicketType
        {
            WorkspaceId = workspaceId,
            Name = trimmedName,
            Color = trimmedColor,
            SortOrder = maxOrder + 1
        };

        this.dbContext.TicketTypes.Add(type);
        await this.dbContext.SaveChangesAsync();
        return type;
    }

    public async Task<TicketType> UpdateTypeAsync(
        int workspaceId,
        int typeId,
        string name,
        string color,
        int sortOrder)
    {
        var type = await this.dbContext.TicketTypes
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == typeId)
            ?? throw new InvalidOperationException(string.Format(null, NotFoundError, "Type"));

        type.Name = ValidateAndTrimName(name, "Type");
        type.Color = TrimColorOrDefault(color);
        type.SortOrder = sortOrder;

        await this.dbContext.SaveChangesAsync();
        return type;
    }

    public async Task DeleteTypeAsync(int workspaceId, int typeId)
    {
        var type = await this.dbContext.TicketTypes
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == typeId);

        if (type != null)
        {
            this.dbContext.TicketTypes.Remove(type);
            await this.dbContext.SaveChangesAsync();
        }
    }

    public async Task<BulkSettingsUpdateResult> BulkUpdateSettingsAsync(int workspaceId, BulkSettingsUpdateRequest request)
    {
        var changedCount = 0;
        var errors = new List<string>();
        WorkspaceEntity? updatedWorkspace = null;

        // Update workspace basic settings if provided
        if (!string.IsNullOrWhiteSpace(request.WorkspaceName) || !string.IsNullOrWhiteSpace(request.WorkspaceSlug))
        {
            var workspace = await this.GetWorkspaceOrThrowAsync(workspaceId);
            var name = !string.IsNullOrWhiteSpace(request.WorkspaceName) ? request.WorkspaceName.Trim() : workspace.Name;
            var slug = !string.IsNullOrWhiteSpace(request.WorkspaceSlug) ? request.WorkspaceSlug.Trim() : workspace.Slug;

            try
            {
                updatedWorkspace = await this.UpdateWorkspaceBasicSettingsAsync(workspaceId, name, slug);
                changedCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
                return new BulkSettingsUpdateResult
                {
                    UpdatedWorkspace = updatedWorkspace,
                    ChangesApplied = changedCount,
                    Errors = errors
                };
            }
        }

        // Get current lists for validation
        var statusList = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId)
            .ToListAsync();
        var priorityList = await this.dbContext.TicketPriorities
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();
        var typeList = await this.dbContext.TicketTypes
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync();

        // Process status updates
        foreach (var statusUpdate in request.StatusUpdates)
        {
            var status = statusList.FirstOrDefault(s => s.Id == statusUpdate.Id);
            if (status == null)
            {
                continue;
            }

            if (statusUpdate.Delete)
            {
                try
                {
                    await this.DeleteStatusAsync(workspaceId, statusUpdate.Id);
                    changedCount++;
                }
                catch (InvalidOperationException)
                {
                    // Ignore deletion errors
                }
                continue;
            }

            var name = !string.IsNullOrWhiteSpace(statusUpdate.Name) ? statusUpdate.Name.Trim() : status.Name;
            var color = GetColorOrDefault(statusUpdate.Color, status.Color);
            var sortOrder = statusUpdate.SortOrder ?? status.SortOrder;
            var isClosedState = statusUpdate.IsClosedState ?? status.IsClosedState;

            try
            {
                await this.UpdateStatusAsync(workspaceId, statusUpdate.Id, name, color, sortOrder, isClosedState);
                changedCount++;
            }
            catch (InvalidOperationException)
            {
                // Ignore update errors
            }
        }

        // Create new status if provided
        if (request.NewStatus != null && !string.IsNullOrWhiteSpace(request.NewStatus.Name))
        {
            try
            {
                await this.AddStatusAsync(workspaceId, request.NewStatus.Name, request.NewStatus.Color, request.NewStatus.IsClosedState);
                changedCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
            }
        }

        // Process priority updates
        foreach (var priorityUpdate in request.PriorityUpdates)
        {
            var priority = priorityList.FirstOrDefault(p => p.Id == priorityUpdate.Id);
            if (priority == null)
            {
                continue;
            }

            if (priorityUpdate.Delete)
            {
                try
                {
                    await this.DeletePriorityAsync(workspaceId, priorityUpdate.Id);
                    changedCount++;
                }
                catch (InvalidOperationException)
                {
                    // Ignore deletion errors
                }
                continue;
            }

            var name = !string.IsNullOrWhiteSpace(priorityUpdate.Name) ? priorityUpdate.Name.Trim() : priority.Name;
            var color = GetColorOrDefault(priorityUpdate.Color, priority.Color);
            var sortOrder = priorityUpdate.SortOrder ?? priority.SortOrder;

            try
            {
                await this.UpdatePriorityAsync(workspaceId, priorityUpdate.Id, name, color, sortOrder);
                changedCount++;
            }
            catch (InvalidOperationException)
            {
                // Ignore update errors
            }
        }

        // Create new priority if provided
        if (request.NewPriority != null && !string.IsNullOrWhiteSpace(request.NewPriority.Name))
        {
            try
            {
                await this.AddPriorityAsync(workspaceId, request.NewPriority.Name, request.NewPriority.Color);
                changedCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
            }
        }

        // Process type updates
        foreach (var typeUpdate in request.TypeUpdates)
        {
            var type = typeList.FirstOrDefault(t => t.Id == typeUpdate.Id);
            if (type == null)
            {
                continue;
            }

            if (typeUpdate.Delete)
            {
                try
                {
                    await this.DeleteTypeAsync(workspaceId, typeUpdate.Id);
                    changedCount++;
                }
                catch (InvalidOperationException)
                {
                    // Ignore deletion errors
                }
                continue;
            }

            var name = !string.IsNullOrWhiteSpace(typeUpdate.Name) ? typeUpdate.Name.Trim() : type.Name;
            var color = GetColorOrDefault(typeUpdate.Color, type.Color);
            var sortOrder = typeUpdate.SortOrder ?? type.SortOrder;

            try
            {
                await this.UpdateTypeAsync(workspaceId, typeUpdate.Id, name, color, sortOrder);
                changedCount++;
            }
            catch (InvalidOperationException)
            {
                // Ignore update errors
            }
        }

        // Create new type if provided
        if (request.NewType != null && !string.IsNullOrWhiteSpace(request.NewType.Name))
        {
            try
            {
                await this.AddTypeAsync(workspaceId, request.NewType.Name, request.NewType.Color);
                changedCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
            }
        }

        return new BulkSettingsUpdateResult
        {
            UpdatedWorkspace = updatedWorkspace,
            ChangesApplied = changedCount,
            Errors = errors
        };
    }

    private async Task<WorkspaceEntity> GetWorkspaceOrThrowAsync(int workspaceId)
    {
        var workspace = await this.dbContext.Workspaces.FindAsync(workspaceId)
            ?? throw new InvalidOperationException(WorkspaceNotFoundError);

        return workspace;
    }

    private async Task ValidateSlugIsAvailableAsync(string slug, int workspaceId)
    {
        var slugLower = slug.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        var existing = await this.dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Slug.Equals(slugLower, StringComparison.OrdinalIgnoreCase));

        if (existing != null && existing.Id != workspaceId)
        {
            throw new InvalidOperationException(SlugInUseError);
        }
    }

    private static string ValidateAndTrimName(string name, string entityType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(string.Format(null, NameRequiredError, entityType));
        }

        return name.Trim();
    }

    private static string TrimColorOrDefault(string color) => string.IsNullOrWhiteSpace(color) ? DefaultColor : color.Trim();

    private static string GetColorOrDefault(string? inputColor, string? currentColor) => !string.IsNullOrWhiteSpace(inputColor) ? inputColor.Trim() : (string.IsNullOrWhiteSpace(currentColor) ? DefaultColor : currentColor);
}




