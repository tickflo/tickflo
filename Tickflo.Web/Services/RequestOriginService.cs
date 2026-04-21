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
        var request = this.httpContextAccessor.HttpContext?.Request;
        if (request == null || !request.Host.HasValue || string.IsNullOrWhiteSpace(request.Scheme))
        {
            return this.config.BaseUrl?.TrimEnd('/') ?? "";
        }

        return $"{request.Scheme}://{request.Host}";
    }
}
