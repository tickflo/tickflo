namespace Tickflo.Core.Services.Notifications;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface INotificationService
{
    public Task CreateAsync(int userId, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null, string? data = null);
    public Task CreateBatchAsync(List<int> userIds, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null);
    public Task SendPendingEmailsAsync(int batchSize = 100);
    public Task SendPendingInAppAsync(int batchSize = 100);
}

public class NotificationService(TickfloDbContext dbContext) : INotificationService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task CreateAsync(int userId, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null, string? data = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = type,
            DeliveryMethod = deliveryMethod,
            Priority = priority,
            Subject = subject,
            Body = body,
            Data = data,
            Status = "pending",
            CreatedBy = createdBy
        };

        this.dbContext.Notifications.Add(notification);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task CreateBatchAsync(List<int> userIds, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null)
    {
        var batchId = Guid.NewGuid().ToString();

        foreach (var userId in userIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Type = type,
                DeliveryMethod = deliveryMethod,
                Priority = priority,
                Subject = subject,
                Body = body,
                Status = "pending",
                BatchId = batchId,
                CreatedBy = createdBy
            };

            this.dbContext.Notifications.Add(notification);
        }

        await this.dbContext.SaveChangesAsync();
    }

    public async Task SendPendingEmailsAsync(int batchSize = 100)
    {
        var pending = await this.dbContext.Notifications
            .Where(n => n.Status == "pending" && n.DeliveryMethod == "email")
            .OrderBy(n => n.CreatedAt)
            .Take(batchSize)
            .ToListAsync();

        foreach (var notification in pending)
        {
            try
            {
                // Get user email - you'll need to inject IUserRepository
                // For now, assuming the email is in the notification data or we need to look it up
                //var toEmail = notification.Data ?? ""; // This should be properly resolved from user

                //await this.emailSenderService.SendAsync(toEmail, notification.Subject, notification.Body);
                notification.Status = "sent";
                notification.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                notification.Status = "failed";
                notification.FailedAt = DateTime.UtcNow;
                notification.FailureReason = ex.Message;
            }
        }

        await this.dbContext.SaveChangesAsync();
    }

    public async Task SendPendingInAppAsync(int batchSize = 100)
    {
        var pending = await this.dbContext.Notifications
            .Where(n => n.Status == "pending" && n.DeliveryMethod == "in_app")
            .OrderBy(n => n.CreatedAt)
            .Take(batchSize)
            .ToListAsync();

        foreach (var notification in pending)
        {
            // In-app notifications are just marked as sent since they're already in the database
            // The UI will query them directly
            notification.Status = "sent";
            notification.SentAt = DateTime.UtcNow;
        }

        await this.dbContext.SaveChangesAsync();
    }
}
