namespace Tickflo.Web.Authentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tickflo.Core.Config;
using Tickflo.Core.Data;

public class TokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TickfloDbContext db,
    TickfloConfig config) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly TickfloDbContext db = db;
    private readonly TickfloConfig config = config;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read token from header or cookie
        var authHeader = this.Request.Headers.Authorization.ToString();
        var tokenValue = !string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.Ordinal)
            ? authHeader["Bearer ".Length..].Trim()
            : this.Request.Cookies[this.config.SessionCookieName];

        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            return AuthenticateResult.NoResult();
        }

        // Load token and user in a single query so the CreatedAt > UpdatedAt
        // invalidation check (see below) does not cost an extra round-trip.
        var tokenQuery = from t in this.db.Tokens
                         join u in this.db.Users on t.UserId equals u.Id
                         where t.Value == tokenValue
                         select new { Token = t, User = u };

        var result = await tokenQuery.FirstOrDefaultAsync();
        if (result == null)
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        var token = result.Token;
        var user = result.User;

        // Use TimeProvider instead of ISystemClock
        var now = this.Options.TimeProvider?.GetUtcNow() ?? TimeProvider.System.GetUtcNow();
        if (token.CreatedAt.AddSeconds(token.MaxAge) < now)
        {
            return AuthenticateResult.Fail("Token expired");
        }

        // Invalidate tokens issued before the user was last updated. This
        // covers both the password-reset use case (SetPasswordWithTokenAsync
        // bumps user.UpdatedAt when the token is consumed) and any other
        // user-profile change that should kick every active session.
        if (user.UpdatedAt.HasValue && token.CreatedAt <= user.UpdatedAt.Value)
        {
            return AuthenticateResult.Fail("Token invalidated by user update");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, $"{token.UserId}"),
        };

        var identity = new ClaimsIdentity(claims, this.Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
