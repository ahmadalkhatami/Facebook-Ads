using Microsoft.EntityFrameworkCore;
using DecisionEngine.Core.Entities;

namespace DecisionEngine.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Creative> Creatives { get; set; }
        public DbSet<Metric> Metrics { get; set; }
        public DbSet<DecisionLog> DecisionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Campaign>().ToTable("campaigns");
            modelBuilder.Entity<Product>().ToTable("products");
            modelBuilder.Entity<Creative>().ToTable("creatives");
            modelBuilder.Entity<Metric>().ToTable("metrics");
            modelBuilder.Entity<DecisionLog>().ToTable("decision_logs");

            // Configure relationships
            modelBuilder.Entity<Creative>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId);

            modelBuilder.Entity<Metric>()
                .HasOne(m => m.Campaign)
                .WithMany()
                .HasForeignKey(m => m.CampaignId);

            modelBuilder.Entity<DecisionLog>()
                .HasOne(d => d.Campaign)
                .WithMany()
                .HasForeignKey(d => d.CampaignId);
        }
    }
}
