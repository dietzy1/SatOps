using Microsoft.EntityFrameworkCore;
using GroundStationEntity = SatOps.Services.GroundStation.GroundStation;
using FlightPlanEntity = SatOps.Services.FlightPlan.FlightPlan;

namespace SatOps.Services
{
    public class SatOpsDbContext : DbContext
    {
        public SatOpsDbContext(DbContextOptions<SatOpsDbContext> options) : base(options)
        {
        }

        // Use  aliases for the DbSet properties
        public DbSet<GroundStationEntity> GroundStations => Set<GroundStationEntity>();
        public DbSet<FlightPlanEntity> FlightPlans => Set<FlightPlanEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure PostGIS extension is enabled and geometry types are configured
            modelBuilder.HasPostgresExtension("postgis");

            modelBuilder.Entity<GroundStationEntity>(entity =>
            {
                entity.ToTable("ground_stations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.HttpUrl).IsRequired();
                // Store as geometry(Point, 4326)
                entity.Property(e => e.Location)
                    .HasColumnType("geometry(Point,4326)")
                    .IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.IsActive).HasDefaultValue(false);
            });

            // Use  alias for the FlightPlan entity configuration
            modelBuilder.Entity<FlightPlanEntity>(entity =>
            {
                entity.ToTable("flight_plans");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Body).HasColumnType("jsonb").IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("timezone('utc', now())");

                // Index for faster lookups by status
                entity.HasIndex(e => e.Status);
            });
        }
    }
}