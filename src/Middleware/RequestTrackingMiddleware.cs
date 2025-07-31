using MyAzureWebApp.Data;
using MyAzureWebApp.Models;
using MyAzureWebApp.Services;
using System.Diagnostics;

namespace MyAzureWebApp.Middleware
{
    /// <summary>
    /// Middleware to track all incoming HTTP requests
    /// </summary>
    public class RequestTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTrackingMiddleware> _logger;

        public RequestTrackingMiddleware(RequestDelegate next, ILogger<RequestTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestTrackingContext dbContext, IUserAgentClassifier userAgentClassifier)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                // Capture request details
                var request = context.Request;
                var userAgent = request.Headers.UserAgent.ToString();
                var (userAgentType, detectedClient) = userAgentClassifier.ClassifyUserAgent(userAgent);

                // Call the next middleware
                await _next(context);

                stopwatch.Stop();

                // Create request log entry
                var requestLog = new RequestLog
                {
                    Timestamp = DateTime.UtcNow,
                    Method = request.Method,
                    Path = request.Path.Value ?? "/",
                    IpAddress = GetClientIpAddress(context),
                    UserAgent = string.IsNullOrEmpty(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 1000)],
                    UserAgentType = userAgentType,
                    DetectedClient = detectedClient,
                    StatusCode = context.Response.StatusCode,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Referer = request.Headers.Referer.FirstOrDefault(),
                    QueryString = request.QueryString.HasValue ? request.QueryString.Value : null
                };

                // Save to database synchronously to avoid context disposal issues
                try
                {
                    dbContext.RequestLogs.Add(requestLog);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save request log to database");
                }

                _logger.LogInformation(
                    "Request tracked: {Method} {Path} - {UserAgentType} ({DetectedClient}) - {StatusCode} - {ProcessingTime}ms",
                    requestLog.Method,
                    requestLog.Path,
                    requestLog.UserAgentType,
                    requestLog.DetectedClient ?? "Unknown",
                    requestLog.StatusCode,
                    requestLog.ProcessingTimeMs
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in request tracking middleware");
                
                // Don't let tracking errors break the request pipeline
                await _next(context);
            }
        }

        private static string? GetClientIpAddress(HttpContext context)
        {
            try
            {
                // Check for forwarded IP first (common in load balancers/proxies)
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // Take the first IP if there are multiple
                    var firstIp = forwardedFor.Split(',')[0].Trim();
                    return firstIp;
                }

                // Check other common headers
                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }

                // Fall back to connection remote IP
                return context.Connection.RemoteIpAddress?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task SaveRequestLogAsync(RequestLog requestLog, RequestTrackingContext dbContext)
        {
            try
            {
                dbContext.RequestLogs.Add(requestLog);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save request log to database");
            }
        }
    }

    /// <summary>
    /// Extension method to register the request tracking middleware
    /// </summary>
    public static class RequestTrackingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestTrackingMiddleware>();
        }
    }
}
