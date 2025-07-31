using System.ComponentModel.DataAnnotations;

namespace MyAzureWebApp.Models
{
    /// <summary>
    /// Represents a logged web request with user agent classification
    /// </summary>
    public class RequestLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Timestamp when the request was made
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// HTTP method (GET, POST, etc.)
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Request path/URL
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Client IP address
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }

        /// <summary>
        /// Raw user agent string
        /// </summary>
        [StringLength(1000)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Classified user agent type
        /// </summary>
        public UserAgentType UserAgentType { get; set; }

        /// <summary>
        /// Detected browser/tool name
        /// </summary>
        [StringLength(100)]
        public string? DetectedClient { get; set; }

        /// <summary>
        /// HTTP response status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Request processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// HTTP referer header
        /// </summary>
        [StringLength(2000)]
        public string? Referer { get; set; }

        /// <summary>
        /// Query string parameters
        /// </summary>
        [StringLength(1000)]
        public string? QueryString { get; set; }
    }

    /// <summary>
    /// Classification of user agent types
    /// </summary>
    public enum UserAgentType
    {
        Unknown = 0,
        Human = 1,
        SearchBot = 2,
        SocialBot = 3,
        ApiTool = 4,
        Crawler = 5,
        Monitor = 6,
        SecurityScanner = 7
    }
}
