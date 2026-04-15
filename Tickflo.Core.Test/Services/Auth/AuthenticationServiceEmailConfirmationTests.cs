namespace Tickflo.CoreTest.Services.Auth;

public class AuthenticationServiceEmailConfirmationTests
{
    [Fact]
    public async Task ResendEmailConfirmationAsyncWhenOriginProvidedShouldUseOriginWithConfirmationRoute()
    {
        var tickfloConfig = new TickfloConfig
        {
            BaseUrl = "https://app.tickflo.co",
            SessionTimeoutMinutes = 20,
        };

        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Test User", "user@example.com", "recovery@example.com", "password-hash");
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var workspaceCreationService = new Mock<IWorkspaceCreationService>();
        var authenticationService = new AuthenticationService(
            databaseContext,
            new Argon2idPasswordHasher(),
            emailSendService.Object,
            tickfloConfig,
            workspaceCreationService.Object);

        await authenticationService.ResendEmailConfirmationAsync(user.Id, "https://localhost:7182");

        var expectedConfirmationLink = $"https://localhost:7182/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={user.EmailConfirmationCode}";
        emailSendService.Verify(service => service.AddToQueueAsync(
            user.Email,
            EmailTemplateType.Signup,
            It.Is<Dictionary<string, string>>(values =>
                values.ContainsKey("confirmation_link") &&
                values["confirmation_link"] == expectedConfirmationLink),
            null), Times.Once);
    }

    [Fact]
    public async Task ResendEmailConfirmationAsyncWhenOriginMissingShouldFallbackToConfiguredBaseUrl()
    {
        var tickfloConfig = new TickfloConfig
        {
            BaseUrl = "https://app.tickflo.co/",
            SessionTimeoutMinutes = 20,
        };

        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Test User", "user@example.com", "recovery@example.com", "password-hash");
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var workspaceCreationService = new Mock<IWorkspaceCreationService>();
        var authenticationService = new AuthenticationService(
            databaseContext,
            new Argon2idPasswordHasher(),
            emailSendService.Object,
            tickfloConfig,
            workspaceCreationService.Object);

        await authenticationService.ResendEmailConfirmationAsync(user.Id);

        var expectedConfirmationLink = $"https://app.tickflo.co/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={user.EmailConfirmationCode}";
        emailSendService.Verify(service => service.AddToQueueAsync(
            user.Email,
            EmailTemplateType.Signup,
            It.Is<Dictionary<string, string>>(values =>
                values.ContainsKey("confirmation_link") &&
                values["confirmation_link"] == expectedConfirmationLink),
            null), Times.Once);
    }

    [Fact]
    public async Task ResendEmailConfirmationAsyncWhenStoredCodeIsMissingShouldGenerateAndUseCode()
    {
        var tickfloConfig = new TickfloConfig
        {
            BaseUrl = "https://app.tickflo.co",
            SessionTimeoutMinutes = 20,
        };

        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Test User", "user@example.com", "recovery@example.com", "password-hash")
        {
            EmailConfirmationCode = null,
        };

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var workspaceCreationService = new Mock<IWorkspaceCreationService>();
        var authenticationService = new AuthenticationService(
            databaseContext,
            new Argon2idPasswordHasher(),
            emailSendService.Object,
            tickfloConfig,
            workspaceCreationService.Object);

        await authenticationService.ResendEmailConfirmationAsync(user.Id, "https://localhost:7182");

        Assert.False(string.IsNullOrWhiteSpace(user.EmailConfirmationCode));

        var expectedConfirmationLink = $"https://localhost:7182/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={user.EmailConfirmationCode}";
        emailSendService.Verify(service => service.AddToQueueAsync(
            user.Email,
            EmailTemplateType.Signup,
            It.Is<Dictionary<string, string>>(values =>
                values.ContainsKey("confirmation_link") &&
                values["confirmation_link"] == expectedConfirmationLink),
            null), Times.Once);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
