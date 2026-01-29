namespace Tickflo.Web.Pages;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;

// TODO: This should NOT be using TickfloDbContext directly. The logic on this page/controller needs moved into a Tickflo.Core service

public class NotificationTicketData
{
    [JsonPropertyName("ticketId")]
    public int TicketId { get; set; }
    [JsonPropertyName("workspaceSlug")]
    public string? WorkspaceSlug { get; set; }
}

[Authorize]
public class NotificationsModel(
    TickfloDbContext dbContext,
    ICurrentUserService currentUserService) : PageModel
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly ICurrentUserService currentUserService = currentUserService;

    public List<Notification> Notifications { get; set; } = [];

    public NotificationTicketData? GetTicketData(Notification notification)
    {
        if (string.IsNullOrEmpty(notification.Data))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<NotificationTicketData>(notification.Data);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        this.Notifications = await this.dbContext.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return this.Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        var notification = await this.dbContext.Notifications.FindAsync(id);
        if (notification == null || notification.UserId != userId)
        {
            return this.NotFound();
        }

        notification.ReadAt = DateTime.UtcNow;
        await this.dbContext.SaveChangesAsync();
        return this.RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Forbid();
        }

        var notifications = await this.dbContext.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ToListAsync();
        foreach (var notification in notifications)
        {
            notification.ReadAt = DateTime.UtcNow;
        }

        await this.dbContext.SaveChangesAsync();
        return this.RedirectToPage();
    }
}
