namespace Tickflo.Core.Services.Web;

public interface IRequestOriginService
{
    // Gets the request origin (scheme + host) from the current HTTP context. Falls back to configuration value if not available
    public string GetCurrentOrigin();
}
