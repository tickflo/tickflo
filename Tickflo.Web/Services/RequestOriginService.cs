namespace Tickflo.Web.Services;

using Microsoft.AspNetCore.Http;
using Tickflo.Core.Config;
using Tickflo.Core.Services.Web;

public class RequestOriginService(IHttpContextAccessor httpContextAccessor, TickfloConfig config) : IRequestOriginService
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly TickfloConfig config = config;

    public string GetCurrentOrigin()
    {
        // Always prefer the configured BaseUrl — it is authoritative (and correct behind a TLS-terminating proxy)
        if (!string.IsNullOrWhiteSpace(this.config.BaseUrl))
        {
            return this.config.BaseUrl.TrimEnd('/');
        }

        var request = this.httpContextAccessor.HttpContext?.Request;
        if (request != null && request.Host.HasValue && !string.IsNullOrWhiteSpace(request.Scheme))
        {
            return $"{request.Scheme}://{request.Host}";
        }

        return "";
    }
}
