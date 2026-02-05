namespace Tickflo.Core.Services.Users;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
/// <summary>
/// Service for managing user invitations and onboarding workflows.
/// </summary>
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;

/// <summary>
/// Service for managing user invitations and onboarding workflows.
/// </summary>
public interface IUserInvitationService
{
    /// <summary>
    /// Invites a user to a workspace with an auto-generated temporary password.
    /// </summary>
    /// <param name="workspaceId">Target workspace</param>
    /// <param name="email">User's email address</param>
    /// <param name="invitedByUserId">User sending the invitation</param>
    /// <param name="roleIds">Role IDs to assign upon acceptance</param>
    public Task InviteUserAsync(
        int workspaceId,
        string email,
        int invitedByUserId,
        List<int> roleIds);

    /// <summary>
    /// Resends an invitation email with a new confirmation code.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="userId">User to resend invitation to</param>
    /// <param name="resentByUserId">User triggering the resend</param>
    public Task ResendInvitationAsync(int workspaceId, int userId, int resentByUserId);

    /// <summary>
    /// Accepts a workspace invitation.
    /// </summary>
    /// <param name="slug">Workspace to accept</param>
    /// <param name="userId">User accepting the invitation</param>
    public Task AcceptInvitationAsync(string slug, int userId);
    public Task DeclineInvitationAsync(string slug, int userId);
}

/// <summary>
/// Result of a user invitation operation.
/// </summary>
public class UserInvitationResult
{
    public User User { get; set; } = null!;
    public string AcceptLink { get; set; } = string.Empty;
    public bool IsNewUser { get; set; }
}

public class UserInvitationService(
    TickfloDbContext dbContext,
    IEmailSendService emailSendService,
    TickfloConfig config) : IUserInvitationService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly TickfloConfig config = config;

    public async Task InviteUserAsync(
        int workspaceId,
        string email,
        int invitedByUserId,
        List<int> roleIds)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required");
        }

        if (roleIds.Count == 0)
        {
            throw new InvalidOperationException("At least one role is required");
        }

        email = email.Trim().ToLowerInvariant();

        // Get workspace for email template
        var workspace = await this.dbContext.Workspaces.FindAsync(workspaceId)
            ?? throw new InvalidOperationException("Workspace not found");

        // Check if user already exists
        var emailLower = email.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        var user = await this.dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.Equals(emailLower, StringComparison.OrdinalIgnoreCase));
        var isNewUser = user == null;

        if (isNewUser)
        {
            user = new User
            {
                Name = email.Split('@')[0],
                Email = email,
                EmailConfirmationCode = SecureTokenGenerator.GenerateToken(16),
                CreatedAt = DateTime.UtcNow
            };

            this.dbContext.Users.Add(user);
            await this.dbContext.SaveChangesAsync();
        }
        else
        {
            var existingMembership = await this.dbContext.UserWorkspaces
                .FirstOrDefaultAsync(uw => uw.UserId == user!.Id && uw.WorkspaceId == workspaceId);

            if (existingMembership != null)
            {
                throw new InvalidOperationException("User is already invited to this workspace");
            }
        }

        if (user == null)
        {
            throw new InvalidOperationException("Failed to create or retrieve user");
        }

        var membership = new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = workspaceId,
            CreatedBy = invitedByUserId,
            Accepted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        this.dbContext.UserWorkspaces.Add(membership);
        await this.dbContext.SaveChangesAsync();

        foreach (var roleId in roleIds)
        {
            var role = await this.dbContext.Roles.FindAsync(roleId);
            if (role == null || role.WorkspaceId != workspaceId)
            {
                throw new InvalidOperationException($"Role with ID {roleId} not found in this workspace");
            }

            var roleAssignment = new UserWorkspaceRole
            {
                UserId = user.Id,
                WorkspaceId = workspaceId,
                RoleId = roleId,
                CreatedBy = invitedByUserId
            };

            this.dbContext.UserWorkspaceRoles.Add(roleAssignment);
        }

        await this.dbContext.SaveChangesAsync();

        if (isNewUser)
        {
            await this.SendNewUserInvitationEmailAsync(workspace, user, invitedByUserId);
        }
        else
        {
            await this.SendExistingUserInvitationEmailAsync(workspace, user, invitedByUserId);
        }
    }

    public async Task ResendInvitationAsync(int workspaceId, int userId, int resentByUserId)
    {
        var workspace = await this.dbContext.Workspaces.FindAsync(workspaceId)
            ?? throw new InvalidOperationException("Workspace not found");

        var membership = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId)
            ?? throw new InvalidOperationException("User is not invited to this workspace");

        var user = await this.dbContext.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        if (user.PasswordHash == null)
        {
            await this.SendNewUserInvitationEmailAsync(
                workspace,
                user,
                resentByUserId);
        }
        else
        {
            await this.SendExistingUserInvitationEmailAsync(
                workspace,
                user,
                resentByUserId);
        }
    }

    public async Task AcceptInvitationAsync(string slug, int userId)
    {
        var slugLower = slug.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        var workspace = await this.dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Slug.Equals(slugLower, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException("Workspace not found");

        var membership = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspace.Id)
            ?? throw new InvalidOperationException("Invitation not found");

        if (membership.Accepted)
        {
            return;
        }

        membership.Accepted = true;
        membership.UpdatedAt = DateTime.UtcNow;
        membership.UpdatedBy = userId;
        await this.dbContext.SaveChangesAsync();
    }

    public async Task DeclineInvitationAsync(string slug, int userId)
    {
        var slugLower = slug.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        var workspace = await this.dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Slug.Equals(slugLower, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException("Workspace not found");

        var membership = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspace.Id)
            ?? throw new InvalidOperationException("Invitation not found");

        if (membership.Accepted)
        {
            throw new InvalidOperationException("Cannot decline an accepted invitation");
        }

        this.dbContext.UserWorkspaces.Remove(membership);
        await this.dbContext.SaveChangesAsync();
    }

    private async Task SendNewUserInvitationEmailAsync(
        Workspace workspace,
        User user,
        int invitedByUserId
    )
    {
        var variables = new Dictionary<string, string>
        {
            { "name", user.Name },
            { "workspace_name", workspace.Name },
            { "signup_link", $"{this.config.BaseUrl}/signup?email={Uri.EscapeDataString(user.Email)}" },
        };

        await this.emailSendService.AddToQueueAsync(
            user.Email,
            EmailTemplateType.WorkspaceInviteNewUser,
            variables,
            invitedByUserId
        );
    }

    private async Task SendExistingUserInvitationEmailAsync(
        Workspace workspace,
        User user,
        int invitedByUserId
    )
    {
        var variables = new Dictionary<string, string>
        {
            { "name", user.Name },
            { "workspace_name", workspace.Name },
            { "login_link", $"{this.config.BaseUrl}/workspaces" },
        };

        await this.emailSendService.AddToQueueAsync(
            user.Email,
            EmailTemplateType.WorkspaceInviteExistingUser,
            variables,
            invitedByUserId
        );
    }
}


