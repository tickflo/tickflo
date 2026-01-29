namespace Tickflo.Web;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

// TODO: This should NOT be using TickfloDbContext directly. The logic on this page/controller needs moved into a Tickflo.Core service

public interface IAppContext
{
    public User? CurrentUser { get; set; }
}

public class AppContext : IAppContext
{
    public User? CurrentUser { get; set; }
}

public class AppContextMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;
    public async Task InvokeAsync(HttpContext context, IAppContext appContext, TickfloDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                appContext.CurrentUser = user;
            }
        }

        await this.next(context);
    }
}
