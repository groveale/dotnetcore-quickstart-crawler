using Microsoft.EntityFrameworkCore;
using MyAzureWebApp.Models;

namespace MyAzureWebApp.Data
{
    /// <summary>
    /// Entity Framework context for request tracking
    /// </summary>
    public class RequestTrackingContext : DbContext
    {
        public RequestTrackingContext(DbContextOptions<RequestTrackingContext> options)
            : base(options)
        {
        }

        public DbSet<RequestLog> RequestLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RequestLog entity
            modelBuilder.Entity<RequestLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_RequestLog_Timestamp");
                entity.HasIndex(e => e.UserAgentType).HasDatabaseName("IX_RequestLog_UserAgentType");
                entity.HasIndex(e => e.IpAddress).HasDatabaseName("IX_RequestLog_IpAddress");
            });
        }
    }
}
