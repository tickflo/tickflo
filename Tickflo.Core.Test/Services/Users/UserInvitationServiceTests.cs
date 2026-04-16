namespace Tickflo.CoreTest.Services.Users;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Web;
using Xunit;

public class UserInvitationServiceTests
{
    [Fact]
    public async Task InviteUserAsyncWhenOriginUsesDevPortForNewUserShouldUseOriginForSignupLink()
    {
        await using var databaseContext = CreateDatabaseContext();
        var inviter = new User("Admin", "admin@example.com", "recovery@example.com", "password-hash");
        databaseContext.Users.Add(inviter);
        await databaseContext.SaveChangesAsync();

        var workspace = new Workspace
        {
            Name = "Operations",
            Slug = "operations",
            CreatedBy = inviter.Id
        };
        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();

        var role = new Role
        {
            WorkspaceId = workspace.Id,
            Name = "Technician",
            Admin = false,
            CreatedBy = inviter.Id,
            CreatedAt = DateTime.UtcNow
        };
        databaseContext.Roles.Add(role);
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var requestOriginService = new Mock<IRequestOriginService>();
        requestOriginService.Setup(service => service.GetCurrentOrigin()).Returns("https://localhost:7182");

        var userInvitationService = new UserInvitationService(
            databaseContext,
            emailSendService.Object,
            requestOriginService.Object);

        await userInvitationService.InviteUserAsync(workspace.Id, "invitee@example.com", inviter.Id, [role.Id]);

        emailSendService.Verify(service => service.AddToQueueAsync(
            "invitee@example.com",
            EmailTemplateType.WorkspaceInviteNewUser,
            It.Is<Dictionary<string, string>>(values =>
                values["signup_link"] == "https://localhost:7182/signup?email=invitee%40example.com"),
            inviter.Id), Times.Once);
    }

    [Fact]
    public async Task InviteUserAsyncWhenOriginUsesDevPortForExistingUserShouldUseOriginForLoginLink()
    {
        await using var databaseContext = CreateDatabaseContext();
        var inviter = new User("Admin", "admin@example.com", "recovery@example.com", "password-hash");
        var existingUser = new User("Existing User", "existing@example.com", "existing-recovery@example.com", "password-hash");
        databaseContext.Users.AddRange(inviter, existingUser);
        await databaseContext.SaveChangesAsync();

        var workspace = new Workspace
        {
            Name = "Operations",
            Slug = "operations",
            CreatedBy = inviter.Id
        };
        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();

        var role = new Role
        {
            WorkspaceId = workspace.Id,
            Name = "Technician",
            Admin = false,
            CreatedBy = inviter.Id,
            CreatedAt = DateTime.UtcNow
        };
        databaseContext.Roles.Add(role);
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var requestOriginService = new Mock<IRequestOriginService>();
        requestOriginService.Setup(service => service.GetCurrentOrigin()).Returns("https://localhost:7182");

        var userInvitationService = new UserInvitationService(
            databaseContext,
            emailSendService.Object,
            requestOriginService.Object);

        await userInvitationService.InviteUserAsync(workspace.Id, existingUser.Email, inviter.Id, [role.Id]);

        emailSendService.Verify(service => service.AddToQueueAsync(
            existingUser.Email,
            EmailTemplateType.WorkspaceInviteExistingUser,
            It.Is<Dictionary<string, string>>(values =>
                values["login_link"] == "https://localhost:7182/workspaces"),
            inviter.Id), Times.Once);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
