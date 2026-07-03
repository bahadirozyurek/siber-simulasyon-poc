using Microsoft.EntityFrameworkCore;
using SiberSimulasyon.Core.Entities;

namespace SiberSimulasyon.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AdminActionLog> AdminActionLogs { get; set; }
        public DbSet<NfcLog> NfcLogs { get; set; }
        public DbSet<CameraFeed> CameraFeeds { get; set; }
        public DbSet<SystemNode> SystemNodes { get; set; }

        public DbSet<User> Users { get; set; }
        
    }
}