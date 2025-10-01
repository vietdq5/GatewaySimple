using System.Collections.Concurrent;

namespace Gateways.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, DateTime> _requests = new();
    private readonly int _requestLimit = 60; // requests per minute
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        
        if (IsRateLimited(clientId))
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use IP address or user ID for rate limiting
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsRateLimited(string clientId)
    {
        var now = DateTime.UtcNow;
        
        // Clean old entries
        var keysToRemove = _requests
            .Where(kvp => now - kvp.Value > _timeWindow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _requests.TryRemove(key, out _);
        }

        // Count requests in the current window
        var requestCount = _requests.Count(kvp => kvp.Key.StartsWith(clientId));
        
        if (requestCount >= _requestLimit)
        {
            return true;
        }

        _requests.TryAdd($"{clientId}_{Guid.NewGuid()}", now);
        return false;
    }
}