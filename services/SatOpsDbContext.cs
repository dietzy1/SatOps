using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using GroundStationEntity = SatOps.Services.GroundStation.GroundStation;
using FlightPlanEntity = SatOps.Services.FlightPlan.FlightPlan;
using SatelliteEntity = SatOps.Services.Satellite.Satellite;

namespace SatOps.Services
{
    public class SatOpsDbContext : DbContext
    {
        public SatOpsDbContext(DbContextOptions<SatOpsDbContext> options) : base(options)
        {
        }

        // Use aliases for the DbSet properties
        public DbSet<GroundStationEntity> GroundStations => Set<GroundStationEntity>();
        public DbSet<FlightPlanEntity> FlightPlans => Set<FlightPlanEntity>();
        public DbSet<SatelliteEntity> Satellites => Set<SatelliteEntity>();

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

            // Use alias for the FlightPlan entity configuration
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

            // Configure Satellite entity
            modelBuilder.Entity<SatelliteEntity>(entity =>
            {
                entity.ToTable("satellites");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.NoradId).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("timezone('utc', now())");

                // Index for faster lookups by NORAD ID and status
                entity.HasIndex(e => e.NoradId).IsUnique();
                entity.HasIndex(e => e.Status);
            });

            // Seed data for Ground Stations
            var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

            modelBuilder.Entity<GroundStationEntity>().HasData(
                new GroundStationEntity
                {
                    Id = 1,
                    Name = "Aarhus",
                    HttpUrl = "http://aarhus-groundstation.example.com",
                    Location = geometryFactory.CreatePoint(new Coordinate(10.2039, 56.1629)), // Aarhus coordinates
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed data for Satellites - ISS
            modelBuilder.Entity<SatelliteEntity>().HasData(
                new SatelliteEntity
                {
                    Id = 1,
                    Name = "International Space Station (ISS)",
                    NoradId = "25544",
                    Status = SatOps.Services.Satellite.SatelliteStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // TLE data for ISS (example - should be updated with current data)
                    TleLine1 = "1 25544U 98067A   23256.90616898  .00020137  00000-0  35438-3 0  9992",
                    TleLine2 = "2 25544  51.6416 339.0970 0003835  48.3825  73.2709 15.50030022414673",
                    LastTleUpdate = DateTime.UtcNow
                }
            );
        }
    }
}