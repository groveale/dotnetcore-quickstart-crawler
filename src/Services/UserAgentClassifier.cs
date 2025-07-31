using MyAzureWebApp.Models;
using System.Text.RegularExpressions;

namespace MyAzureWebApp.Services
{
    /// <summary>
    /// Service for classifying user agents and detecting client types
    /// </summary>
    public interface IUserAgentClassifier
    {
        (UserAgentType Type, string? DetectedClient) ClassifyUserAgent(string? userAgent);
    }

    public class UserAgentClassifier : IUserAgentClassifier
    {
        private readonly ILogger<UserAgentClassifier> _logger;

        // Pre-compiled regex patterns for performance
        private static readonly Dictionary<UserAgentType, List<Regex>> UserAgentPatterns = new()
        {
            [UserAgentType.SearchBot] = new List<Regex>
            {
                new(@"Googlebot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Bingbot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Slurp", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"DuckDuckBot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Baiduspider", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"YandexBot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"facebookexternalhit", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"spider", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"crawler", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            [UserAgentType.SocialBot] = new List<Regex>
            {
                new(@"facebookexternalhit", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Twitterbot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"LinkedInBot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"WhatsApp", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"TelegramBot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Discordbot", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            [UserAgentType.ApiTool] = new List<Regex>
            {
                new(@"Insomnia", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Postman", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"curl", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"wget", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"HTTPie", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Thunder Client", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Paw", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"RestSharp", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"okhttp", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"python-requests", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"node-fetch", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"axios", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            [UserAgentType.Monitor] = new List<Regex>
            {
                new(@"UptimeRobot", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Pingdom", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"StatusCake", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Site24x7", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"monitor", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"uptime", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            [UserAgentType.SecurityScanner] = new List<Regex>
            {
                new(@"Nessus", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"OpenVAS", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Qualys", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Nmap", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"sqlmap", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Nikto", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"scanner", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            [UserAgentType.Crawler] = new List<Regex>
            {
                new(@"Scrapy", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"BeautifulSoup", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"Selenium", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"PhantomJS", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"HeadlessChrome", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"bot", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        };

        // Browser patterns for human detection
        private static readonly List<Regex> BrowserPatterns = new()
        {
            new(@"Chrome/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"Firefox/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"Safari/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"Edge/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"Opera/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"Mozilla/", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        public UserAgentClassifier(ILogger<UserAgentClassifier> logger)
        {
            _logger = logger;
        }

        public (UserAgentType Type, string? DetectedClient) ClassifyUserAgent(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return (UserAgentType.Unknown, null);
            }

            try
            {
                // Check each category in order of specificity
                foreach (var category in UserAgentPatterns)
                {
                    foreach (var pattern in category.Value)
                    {
                        var match = pattern.Match(userAgent);
                        if (match.Success)
                        {
                            var detectedClient = ExtractClientName(userAgent, match.Value);
                            _logger.LogDebug("Classified user agent as {Type}: {UserAgent}", category.Key, userAgent);
                            return (category.Key, detectedClient);
                        }
                    }
                }

                // Check if it looks like a human browser
                foreach (var browserPattern in BrowserPatterns)
                {
                    var match = browserPattern.Match(userAgent);
                    if (match.Success)
                    {
                        var detectedClient = ExtractBrowserName(userAgent);
                        _logger.LogDebug("Classified user agent as Human browser: {UserAgent}", userAgent);
                        return (UserAgentType.Human, detectedClient);
                    }
                }

                _logger.LogDebug("Could not classify user agent: {UserAgent}", userAgent);
                return (UserAgentType.Unknown, userAgent.Length > 50 ? userAgent[..50] + "..." : userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying user agent: {UserAgent}", userAgent);
                return (UserAgentType.Unknown, null);
            }
        }

        private static string ExtractClientName(string userAgent, string matchedPattern)
        {
            // Try to extract a more specific client name
            var commonClients = new Dictionary<string, string>
            {
                ["Insomnia"] = "Insomnia",
                ["Postman"] = "Postman",
                ["curl"] = "cURL",
                ["wget"] = "Wget",
                ["Googlebot"] = "Google Bot",
                ["Bingbot"] = "Bing Bot",
                ["facebookexternalhit"] = "Facebook Bot",
                ["Twitterbot"] = "Twitter Bot",
                ["UptimeRobot"] = "UptimeRobot",
                ["Pingdom"] = "Pingdom"
            };

            foreach (var client in commonClients)
            {
                if (userAgent.Contains(client.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return client.Value;
                }
            }

            return matchedPattern;
        }

        private static string ExtractBrowserName(string userAgent)
        {
            if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase) && 
                !userAgent.Contains("Chromium", StringComparison.OrdinalIgnoreCase))
                return "Chrome";
            
            if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                return "Firefox";
            
            if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase) && 
                !userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                return "Safari";
            
            if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
                return "Edge";
            
            if (userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
                return "Opera";

            return "Browser";
        }
    }
}
