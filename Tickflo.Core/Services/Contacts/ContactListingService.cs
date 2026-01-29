namespace Tickflo.Core.Services.Contacts;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IContactListingService
{
    /// <summary>
    /// Gets filtered contacts for a workspace with optional priority and search filtering.
    /// </summary>
    public Task<(IReadOnlyList<Contact> Items, IReadOnlyList<TicketPriority> Priorities)> GetListAsync(
        int workspaceId,
        string? priorityFilter = null,
        string? searchQuery = null);
}


public class ContactListingService(TickfloDbContext dbContext) : IContactListingService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<(IReadOnlyList<Contact> Items, IReadOnlyList<TicketPriority> Priorities)> GetListAsync(
        int workspaceId,
        string? priorityFilter = null,
        string? searchQuery = null)
    {
        var allContacts = await this.dbContext.Contacts
            .Where(c => c.WorkspaceId == workspaceId)
            .ToListAsync();

        var filtered = FilterContacts(allContacts, priorityFilter, searchQuery);

        var priorities = await this.dbContext.TicketPriorities
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();

        return (filtered.ToList(), priorities);
    }

    private static IEnumerable<Contact> FilterContacts(
        IEnumerable<Contact> contacts,
        string? priorityFilter,
        string? searchQuery)
    {
        var result = contacts;

        if (!string.IsNullOrWhiteSpace(priorityFilter))
        {
            result = result.Where(c =>
                string.Equals(c.Priority, priorityFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var trimmedQuery = searchQuery.Trim();
            result = result.Where(c =>
                (!string.IsNullOrWhiteSpace(c.Name) && c.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Email) && c.Email.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Company) && c.Company.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            );
        }

        return result;
    }
}

