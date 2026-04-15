namespace Tickflo.Web.Services;

using Microsoft.AspNetCore.Http;

public interface IRequestOriginService
{
    public string? GetCurrentOrigin(HttpRequest request);
}

public class RequestOriginService : IRequestOriginService
{
    public string? GetCurrentOrigin(HttpRequest request)
    {
        if (!request.Host.HasValue || string.IsNullOrWhiteSpace(request.Scheme))
        {
            return null;
        }

        return $"{request.Scheme}://{request.Host}";
    }
}
