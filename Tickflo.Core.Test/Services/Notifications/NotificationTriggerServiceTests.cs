namespace Tickflo.CoreTest.Services.Notifications;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Web;
using Xunit;

public class NotificationTriggerServiceTests
{
    [Fact]
    public async Task NotifyTicketCreatedAsync_WhenTicketIsAssignedToAnotherUser_ShouldQueueAssignmentEmail()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var creator = new User("Dispatcher", "dispatcher@example.com", "dispatcher-recovery@example.com", "password-hash");
        var assignee = new User("Tech", "tech@example.com", "tech-recovery@example.com", "password-hash");

        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.AddRange(creator, assignee);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = 42,
            WorkspaceId = workspace.Id,
            Subject = "Replace rooftop unit",
            AssignedUserId = assignee.Id
        };

        var emailSendService = new Mock<IEmailSendService>();
        var notificationTriggerService = CreateNotificationTriggerService(databaseContext, emailSendService);

        await notificationTriggerService.NotifyTicketCreatedAsync(workspace.Id, ticket, creator.Id);

        emailSendService.Verify(service => service.AddToQueueAsync(
            assignee.Email,
            EmailTemplateType.TicketAssigned,
            It.Is<Dictionary<string, string>>(values =>
                values["recipient_name"] == assignee.Name &&
                values["actor_name"] == creator.Name &&
                values["workspace_name"] == workspace.Name &&
                values["ticket_id"] == ticket.Id.ToString() &&
                values["ticket_subject"] == ticket.Subject &&
                values["ticket_link"] == $"https://app.tickflo.co/workspaces/{workspace.Slug}/tickets/{ticket.Id}" &&
                values["change_summary"] == "You have been assigned this ticket."),
            creator.Id), Times.Once);
    }

    [Fact]
    public async Task NotifyTicketAssignmentChangedAsync_WhenActorAssignsTicketToSelf_ShouldQueueAssignmentEmail()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var assignee = new User("Tech", "tech@example.com", "tech-recovery@example.com", "password-hash");

        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.Add(assignee);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = 99,
            WorkspaceId = workspace.Id,
            Subject = "Inspect rooftop unit",
            AssignedUserId = assignee.Id
        };

        var emailSendService = new Mock<IEmailSendService>();
        var notificationTriggerService = CreateNotificationTriggerService(databaseContext, emailSendService);

        await notificationTriggerService.NotifyTicketAssignmentChangedAsync(workspace.Id, ticket, null, null, assignee.Id);

        emailSendService.Verify(service => service.AddToQueueAsync(
            assignee.Email,
            EmailTemplateType.TicketAssigned,
            It.Is<Dictionary<string, string>>(values =>
                values["recipient_name"] == assignee.Name &&
                values["actor_name"] == assignee.Name &&
                values["workspace_name"] == workspace.Name &&
                values["ticket_id"] == ticket.Id.ToString() &&
                values["ticket_subject"] == ticket.Subject &&
                values["ticket_link"] == $"https://app.tickflo.co/workspaces/{workspace.Slug}/tickets/{ticket.Id}" &&
                values["change_summary"] == "You have been assigned this ticket."),
            assignee.Id), Times.Once);
    }

    [Fact]
    public async Task NotifyTicketUpdatedAsync_WhenCreatorAndAssigneeNeedNotification_ShouldQueueOneEmailEach()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var creator = new User("Creator", "creator@example.com", "creator-recovery@example.com", "password-hash");
        var assignee = new User("Assignee", "assignee@example.com", "assignee-recovery@example.com", "password-hash");
        var updater = new User("Coordinator", "coordinator@example.com", "coordinator-recovery@example.com", "password-hash");

        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.AddRange(creator, assignee, updater);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = 314,
            WorkspaceId = workspace.Id,
            Subject = "Repair lighting",
            AssignedUserId = assignee.Id
        };

        databaseContext.TicketHistory.Add(new TicketHistory
        {
            WorkspaceId = workspace.Id,
            TicketId = ticket.Id,
            CreatedByUserId = creator.Id,
            Action = TicketHistoryAction.Created,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var notificationTriggerService = CreateNotificationTriggerService(databaseContext, emailSendService);

        await notificationTriggerService.NotifyTicketUpdatedAsync(
            workspace.Id,
            ticket,
            updater.Id,
            "Status changed from 'New' to 'In Progress'.");

        emailSendService.Verify(service => service.AddToQueueAsync(
            creator.Email,
            EmailTemplateType.TicketUpdated,
            It.Is<Dictionary<string, string>>(values =>
                values["recipient_name"] == creator.Name &&
                values["actor_name"] == updater.Name &&
                values["change_summary"] == "Status changed from 'New' to 'In Progress'."),
            updater.Id), Times.Once);

        emailSendService.Verify(service => service.AddToQueueAsync(
            assignee.Email,
            EmailTemplateType.TicketUpdated,
            It.Is<Dictionary<string, string>>(values =>
                values["recipient_name"] == assignee.Name &&
                values["actor_name"] == updater.Name &&
                values["change_summary"] == "Status changed from 'New' to 'In Progress'."),
            updater.Id), Times.Once);
    }

    [Fact]
    public async Task NotifyTicketUpdatedAsync_WhenAssigneeIsExcluded_ShouldOnlyNotifyCreator()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var creator = new User("Creator", "creator@example.com", "creator-recovery@example.com", "password-hash");
        var assignee = new User("Assignee", "assignee@example.com", "assignee-recovery@example.com", "password-hash");
        var updater = new User("Coordinator", "coordinator@example.com", "coordinator-recovery@example.com", "password-hash");

        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.AddRange(creator, assignee, updater);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = 2718,
            WorkspaceId = workspace.Id,
            Subject = "Inspect fire panel",
            AssignedUserId = assignee.Id
        };

        databaseContext.TicketHistory.Add(new TicketHistory
        {
            WorkspaceId = workspace.Id,
            TicketId = ticket.Id,
            CreatedByUserId = creator.Id,
            Action = TicketHistoryAction.Created,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var notificationTriggerService = CreateNotificationTriggerService(databaseContext, emailSendService);

        await notificationTriggerService.NotifyTicketUpdatedAsync(
            workspace.Id,
            ticket,
            updater.Id,
            "Assignment changed.",
            [assignee.Id]);

        emailSendService.Verify(service => service.AddToQueueAsync(
            creator.Email,
            EmailTemplateType.TicketUpdated,
            It.IsAny<Dictionary<string, string>>(),
            updater.Id), Times.Once);
        emailSendService.Verify(service => service.AddToQueueAsync(
            assignee.Email,
            EmailTemplateType.TicketUpdated,
            It.IsAny<Dictionary<string, string>>(),
            updater.Id), Times.Never);
    }

    [Fact]
    public async Task NotifyTicketCommentAddedAsync_WhenCommentIsClientVisible_ShouldQueueCommentEmailsForCreatorAssigneeAndContactOwner()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var creator = new User("Creator", "creator@example.com", "creator-recovery@example.com", "password-hash");
        var assignee = new User("Assignee", "assignee@example.com", "assignee-recovery@example.com", "password-hash");
        var commenter = new User("Commenter", "commenter@example.com", "commenter-recovery@example.com", "password-hash");
        var contactOwner = new User("Account Manager", "owner@example.com", "owner-recovery@example.com", "password-hash");

        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.AddRange(creator, assignee, commenter, contactOwner);
        await databaseContext.SaveChangesAsync();

        var contact = new Contact
        {
            WorkspaceId = workspace.Id,
            Name = "Customer",
            Email = "customer@example.com",
            AssignedUserId = contactOwner.Id
        };

        databaseContext.Contacts.Add(contact);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = 5150,
            WorkspaceId = workspace.Id,
            Subject = "Check backup generator",
            AssignedUserId = assignee.Id,
            ContactId = contact.Id
        };

        databaseContext.TicketHistory.Add(new TicketHistory
        {
            WorkspaceId = workspace.Id,
            TicketId = ticket.Id,
            CreatedByUserId = creator.Id,
            Action = TicketHistoryAction.Created,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3)
        });
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var notificationTriggerService = CreateNotificationTriggerService(databaseContext, emailSendService);

        await notificationTriggerService.NotifyTicketCommentAddedAsync(workspace.Id, ticket, commenter.Id, true);

        emailSendService.Verify(service => service.AddToQueueAsync(
            creator.Email,
            EmailTemplateType.TicketComment,
            It.Is<Dictionary<string, string>>(values => values["change_summary"] == "A new client-visible comment was added."),
            commenter.Id), Times.Once);
        emailSendService.Verify(service => service.AddToQueueAsync(
            assignee.Email,
            EmailTemplateType.TicketComment,
            It.Is<Dictionary<string, string>>(values => values["change_summary"] == "A new client-visible comment was added."),
            commenter.Id), Times.Once);
        emailSendService.Verify(service => service.AddToQueueAsync(
            contactOwner.Email,
            EmailTemplateType.TicketComment,
            It.Is<Dictionary<string, string>>(values => values["change_summary"] == "A new client-visible comment was added."),
            commenter.Id), Times.Once);
    }

    [Fact]
    public async Task NotifyTicketCreatedAsync_WhenRequestOriginUsesDevPort_ShouldUseOriginForTicketLink()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var creator = new User("Dispatcher", "dispatcher@example.com", "dispatcher-recovery@example.com", "password-hash");
        var assignee = new User("Tech", "tech@example.com", "tech-recovery@example.com", "password-hash");

        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.AddRange(creator, assignee);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = 77,
            WorkspaceId = workspace.Id,
            Subject = "Inspect RTU",
            AssignedUserId = assignee.Id
        };

        var emailSendService = new Mock<IEmailSendService>();
        var notificationTriggerService = CreateNotificationTriggerService(
            databaseContext,
            emailSendService,
            "https://localhost:7182");

        await notificationTriggerService.NotifyTicketCreatedAsync(workspace.Id, ticket, creator.Id);

        emailSendService.Verify(service => service.AddToQueueAsync(
            assignee.Email,
            EmailTemplateType.TicketAssigned,
            It.Is<Dictionary<string, string>>(values =>
                values["ticket_link"] == $"https://localhost:7182/workspaces/{workspace.Slug}/tickets/{ticket.Id}"),
            creator.Id), Times.Once);
    }

    private static NotificationTriggerService CreateNotificationTriggerService(
        TickfloDbContext databaseContext,
        Mock<IEmailSendService> emailSendService,
        string origin = "https://app.tickflo.co")
    {
        var tickfloConfig = new TickfloConfig
        {
            BaseUrl = "https://app.tickflo.co"
        };

        var requestOriginService = new Mock<IRequestOriginService>();
        requestOriginService.Setup(service => service.GetCurrentOrigin()).Returns(origin);

        return new NotificationTriggerService(databaseContext, emailSendService.Object, tickfloConfig, requestOriginService.Object);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
