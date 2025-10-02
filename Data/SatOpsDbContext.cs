using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using GroundStationEntity = SatOps.Modules.Groundstation.GroundStation;
using FlightPlanEntity = SatOps.Modules.Schedule.FlightPlan;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;
using UserEntity = SatOps.Modules.User.User;
using TelemetryDataEntity = SatOps.Modules.Operation.TelemetryData;
using ImageDataEntity = SatOps.Modules.Operation.ImageData;
using OverpassEntity = SatOps.Modules.Overpass.Entity;

namespace SatOps.Data
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
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<TelemetryDataEntity> TelemetryData => Set<TelemetryDataEntity>();
        public DbSet<ImageDataEntity> ImageData => Set<ImageDataEntity>();
        public DbSet<OverpassEntity> Overpasses => Set<OverpassEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GroundStationEntity>(entity =>
            {
                entity.ToTable("ground_stations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn(); // PostgreSQL serial
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.HttpUrl).IsRequired();
                entity.HasIndex(e => e.ApplicationId).IsUnique();

                // Configure Location as owned entity
                entity.OwnsOne(e => e.Location, location =>
                {
                    location.Property(l => l.Latitude).IsRequired().HasColumnName("latitude");
                    location.Property(l => l.Longitude).IsRequired().HasColumnName("longitude");
                    location.Property(l => l.Altitude).HasColumnName("altitude").HasDefaultValue(0);
                });

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.IsActive).HasDefaultValue(false);
            });

            // Use alias for the FlightPlan entity configuration
            modelBuilder.Entity<FlightPlanEntity>(entity =>
            {
                entity.ToTable("flight_plans");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn(); // PostgreSQL serial
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
                entity.Property(e => e.Id).UseIdentityByDefaultColumn(); // PostgreSQL serial
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.NoradId).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.LastUpdate).HasDefaultValueSql("timezone('utc', now())");

                // Index for faster lookups by NORAD ID and status
                entity.HasIndex(e => e.NoradId).IsUnique();
                entity.HasIndex(e => e.Status);
            });

            // Seed data for Ground Stations
            modelBuilder.Entity<GroundStationEntity>().HasData(
                new GroundStationEntity
                {
                    Id = 1,
                    Name = "Aarhus",
                    HttpUrl = "http://aarhus-groundstation.example.com",
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Configure seed data for Location using OwnsOne
            modelBuilder.Entity<GroundStationEntity>()
                .OwnsOne(e => e.Location)
                .HasData(new
                {
                    GroundStationId = 1,
                    Latitude = 56.17197289799066, // Aarhus coordinates
                    Longitude = 10.191659216036516,
                    Altitude = 62.0 // 205ft might need to add more to account for building height
                });

            // Configure Users entity
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("timezone('utc', now())");

                entity.Property(e => e.AdditionalScopes).HasColumnType("jsonb");
                entity.Property(e => e.AdditionalRoles).HasColumnType("jsonb");

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Role);
            });

            // Configure TelemetryData entity
            modelBuilder.Entity<TelemetryDataEntity>(entity =>
            {
                entity.ToTable("telemetry_data");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn(); // PostgreSQL serial
                entity.Property(e => e.GroundStationId).IsRequired();
                entity.Property(e => e.SatelliteId).IsRequired();
                entity.Property(e => e.FlightPlanId).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.S3ObjectPath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileSize).IsRequired();
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ReceivedAt).HasDefaultValueSql("timezone('utc', now())");

                // Indexes for faster lookups
                entity.HasIndex(e => e.GroundStationId);
                entity.HasIndex(e => e.SatelliteId);
                entity.HasIndex(e => e.FlightPlanId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ReceivedAt);
            });

            // Configure ImageData entity
            modelBuilder.Entity<ImageDataEntity>(entity =>
            {
                entity.ToTable("image_data");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn(); // PostgreSQL serial
                entity.Property(e => e.SatelliteId).IsRequired();
                entity.Property(e => e.GroundStationId).IsRequired();
                entity.Property(e => e.CaptureTime).IsRequired();
                entity.Property(e => e.S3ObjectPath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileSize).IsRequired();
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ReceivedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.Latitude).HasPrecision(9, 6);
                entity.Property(e => e.Longitude).HasPrecision(9, 6);
                entity.Property(e => e.Metadata).HasColumnType("jsonb");

                // Indexes for faster lookups
                entity.HasIndex(e => e.SatelliteId);
                entity.HasIndex(e => e.GroundStationId);
                entity.HasIndex(e => e.CaptureTime);
                entity.HasIndex(e => e.ReceivedAt);
                entity.HasIndex(e => new { e.Latitude, e.Longitude });
            });

            // Configure Overpass entity
            modelBuilder.Entity<OverpassEntity>(entity =>
            {
                entity.ToTable("overpasses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn(); // PostgreSQL serial
                entity.Property(e => e.SatelliteId).IsRequired();
                entity.Property(e => e.GroundStationId).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.MaxElevationTime).IsRequired();
                entity.Property(e => e.MaxElevation).IsRequired();
                entity.Property(e => e.DurationSeconds).IsRequired();
                entity.Property(e => e.StartAzimuth).IsRequired();
                entity.Property(e => e.EndAzimuth).IsRequired();

                // Indexes for faster lookups
                entity.HasIndex(e => e.SatelliteId);
                entity.HasIndex(e => e.GroundStationId);
                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.EndTime);
                entity.HasIndex(e => new { e.SatelliteId, e.GroundStationId, e.StartTime });
            });

            // Seed data for Satellites - ISS
            modelBuilder.Entity<SatelliteEntity>().HasData(
                new SatelliteEntity
                {
                    Id = 1,
                    Name = "International Space Station (ISS)",
                    NoradId = 25544,
                    Status = Modules.Satellite.SatelliteStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow,
                    TleLine1 = "1 25544U 98067A   23256.90616898  .00020137  00000-0  35438-3 0  9992",
                    TleLine2 = "2 25544  51.6416 339.0970 0003835  48.3825  73.2709 15.50030022414673"
                },
                new SatelliteEntity
                {
                    Id = 2,
                    Name = "SENTINEL-2C",
                    NoradId = 60989,
                    Status = Modules.Satellite.SatelliteStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow,
                    TleLine1 = "1 60989U 24157A   25270.79510520  .00000303  00000-0  13232-3 0  9996",
                    TleLine2 = "2 60989  98.5675 344.4033 0001006  86.9003 273.2295 14.30815465 55465"
                }
            );
        }
    }
}