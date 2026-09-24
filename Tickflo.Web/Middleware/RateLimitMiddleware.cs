namespace Tickflo.Web.Middleware;

using System.Collections.Concurrent;

/// <summary>
/// Simple in-memory rate limiting middleware for auth endpoints.
/// Tracks requests per IP per endpoint with a fixed window.
/// </summary>
public class RateLimitMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> Buckets = new();
    private const int MaxRequests = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    // Tracks when the expired-bucket sweep last ran (per process), so we prune at most
    // once per window and the static store stays bounded.
    private static long lastPruneUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // Auth-related paths that should be rate-limited
    private static readonly string[] AuthPaths =
    [
        "/login",
        "/signup",
        "/forgot-password",
        "/reset-password",
        "/set-password",
        "/api/send-emails",
        "/email-confirmation/confirm",
        "/email-confirmation/resend"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        PruneExpiredBuckets();

        if (path != null && PathRequiresRateLimiting(path))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{ip}:{path}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var entry = Buckets.GetOrAdd(key, _ => new RateLimitEntry { Count = 0 });
            var blocked = false;

            lock (entry)
            {
                if (now - entry.WindowStartUnixMs > (long)Window.TotalMilliseconds)
                {
                    entry.WindowStartUnixMs = now;
                    entry.Count = 0;
                }

                if (entry.Count >= MaxRequests)
                {
                    blocked = true;
                }
                else
                {
                    entry.Count++;
                }
            }

            if (blocked)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." });
                return;
            }
        }

        await this.next(context);
    }

    private static bool PathRequiresRateLimiting(string path)
    {
        foreach (var authPath in AuthPaths)
        {
            if (path.StartsWith(authPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes buckets that have been idle for longer than the rate-limit window so the
    /// static store does not grow without bound. Runs at most once per window.
    /// </summary>
    private static void PruneExpiredBuckets()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var last = Interlocked.Read(ref lastPruneUnixMs);

        if (now - last < (long)Window.TotalMilliseconds)
        {
            return;
        }

        // Claim the sweep so concurrent requests don't all prune simultaneously.
        if (Interlocked.CompareExchange(ref lastPruneUnixMs, now, last) != last)
        {
            return;
        }

        var cutoff = now - (long)Window.TotalMilliseconds;
        foreach (var bucketKey in Buckets.Keys)
        {
            if (Buckets.TryGetValue(bucketKey, out var entry) && entry.WindowStartUnixMs < cutoff)
            {
                Buckets.TryRemove(bucketKey, out _);
            }
        }
    }

    private sealed class RateLimitEntry
    {
        public long WindowStartUnixMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public int Count { get; set; }
    }
}
