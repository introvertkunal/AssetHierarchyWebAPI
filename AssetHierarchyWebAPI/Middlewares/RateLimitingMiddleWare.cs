using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AssetHierarchyWebAPI.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private static DateTime _lastApiCallTime = DateTime.MinValue;
        private static readonly TimeSpan _limitDuration = TimeSpan.FromMinutes(1); 

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var method = context.Request.Method.ToUpperInvariant();
            var path = context.Request.Path.Value?.ToLowerInvariant();

            bool isRateLimitedEndpoint =
                method == "POST" && path == "/api/asset/add" ||
                method == "DELETE" && path == "/api/asset/remove";

            if (isRateLimitedEndpoint)
            {
                var now = DateTime.UtcNow;
                if (now - _lastApiCallTime < _limitDuration)
                {
                    var waitTime = _limitDuration - (now - _lastApiCallTime);
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync($"Rate limit exceeded. Try again in {waitTime.Seconds} seconds.");
                    return;
                }

                _lastApiCallTime = now;
            }

            await _next(context);
        }
    }
}
