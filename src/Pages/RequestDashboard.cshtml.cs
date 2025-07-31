using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyAzureWebApp.Data;
using MyAzureWebApp.Models;

namespace MyAzureWebApp.Pages
{
    public class RequestDashboardModel : PageModel
    {
        private readonly RequestTrackingContext _context;
        private readonly ILogger<RequestDashboardModel> _logger;

        public RequestDashboardModel(RequestTrackingContext context, ILogger<RequestDashboardModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<RequestLog> RecentRequests { get; set; } = new();
        public Dictionary<UserAgentType, int> UserAgentStats { get; set; } = new();
        public Dictionary<string, int> TopClients { get; set; } = new();
        public Dictionary<string, int> TopPaths { get; set; } = new();
        public int TotalRequests { get; set; }
        public double AverageProcessingTime { get; set; }

        public async Task OnGetAsync(int page = 1, int pageSize = 50)
        {
            try
            {
                // Get recent requests with pagination
                RecentRequests = await _context.RequestLogs
                    .OrderByDescending(r => r.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Calculate statistics for the last 24 hours
                var yesterday = DateTime.UtcNow.AddDays(-1);
                var recentLogs = await _context.RequestLogs
                    .Where(r => r.Timestamp >= yesterday)
                    .ToListAsync();

                // User agent type distribution
                UserAgentStats = recentLogs
                    .GroupBy(r => r.UserAgentType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Top detected clients
                TopClients = recentLogs
                    .Where(r => !string.IsNullOrEmpty(r.DetectedClient))
                    .GroupBy(r => r.DetectedClient!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Top requested paths
                TopPaths = recentLogs
                    .GroupBy(r => r.Path)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Overall stats
                TotalRequests = recentLogs.Count;
                AverageProcessingTime = recentLogs.Any() ? recentLogs.Average(r => r.ProcessingTimeMs) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request dashboard data");
                // Initialize empty collections to prevent errors in the view
                RecentRequests = new List<RequestLog>();
                UserAgentStats = new Dictionary<UserAgentType, int>();
                TopClients = new Dictionary<string, int>();
                TopPaths = new Dictionary<string, int>();
            }
        }
    }
}
