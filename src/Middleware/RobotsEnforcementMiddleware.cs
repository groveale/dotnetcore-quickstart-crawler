using System.Text.RegularExpressions;

namespace MyAzureWebApp.Middleware
{
    /// <summary>
    /// Middleware to enforce robots.txt rules and block specific user agents
    /// </summary>
    public class RobotsEnforcementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RobotsEnforcementMiddleware> _logger;

        // The specific user agent we want to block (Microsoft Graph Connectors)
        private static readonly string BlockedUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko; GraphConnectors) Chrome/76.0.3809.132 Safari/537.36";
        
        // Paths that are blocked for the specific user agent
        private static readonly HashSet<string> BlockedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/RequestDashboard",
            "/TestApi",
            "/Privacy",
            "/Error"
        };

        // Paths that start with these prefixes are also blocked
        private static readonly string[] BlockedPathPrefixes = 
        {
            "/lib/",
            "/css/",
            "/js/"
        };

        public RobotsEnforcementMiddleware(RequestDelegate next, ILogger<RobotsEnforcementMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a request for robots.txt - always allow
            if (context.Request.Path.StartsWithSegments("/robots.txt", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var userAgent = context.Request.Headers.UserAgent.ToString();
            var requestPath = context.Request.Path.Value ?? "/";

            // Check if this is the blocked user agent
            if (IsBlockedUserAgent(userAgent))
            {
                // Check if the path is blocked
                if (IsPathBlocked(requestPath))
                {
                    _logger.LogWarning(
                        "Blocked request from Microsoft Graph Connectors to {Path}. User-Agent: {UserAgent}",
                        requestPath,
                        userAgent
                    );

                    // Return 403 Forbidden with a custom message
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(
                        "Access denied. This resource is not available to Microsoft Graph Connectors. " +
                        "Please check robots.txt for allowed paths."
                    );
                    return;
                }
                else
                {
                    _logger.LogInformation(
                        "Allowed request from Microsoft Graph Connectors to {Path} (root page allowed)",
                        requestPath
                    );
                }
            }

            // Continue to next middleware
            await _next(context);
        }

        private static bool IsBlockedUserAgent(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;

            // Check for exact match or if it contains the key GraphConnectors identifier
            return userAgent.Contains("GraphConnectors", StringComparison.OrdinalIgnoreCase) ||
                   userAgent.Equals(BlockedUserAgent, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPathBlocked(string path)
        {
            // Root path is always allowed
            if (path == "/")
                return false;

            // Check exact path matches
            if (BlockedPaths.Contains(path))
                return true;

            // Check path prefixes
            foreach (var prefix in BlockedPathPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Extension method to register the robots enforcement middleware
    /// </summary>
    public static class RobotsEnforcementMiddlewareExtensions
    {
        public static IApplicationBuilder UseRobotsEnforcement(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RobotsEnforcementMiddleware>();
        }
    }
}
