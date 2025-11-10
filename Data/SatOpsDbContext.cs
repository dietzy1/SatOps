using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GroundStationEntity = SatOps.Modules.Groundstation.GroundStation;
using FlightPlanEntity = SatOps.Modules.FlightPlan.FlightPlan;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;
using UserEntity = SatOps.Modules.User.User;
using ImageDataEntity = SatOps.Modules.GroundStationLink.ImageData;
using OverpassEntity = SatOps.Modules.Overpass.Entity;

namespace SatOps.Data
{
    public class SatOpsDbContext(DbContextOptions<SatOpsDbContext> options) : DbContext(options)
    {
        public DbSet<GroundStationEntity> GroundStations => Set<GroundStationEntity>();
        public DbSet<FlightPlanEntity> FlightPlans => Set<FlightPlanEntity>();
        public DbSet<SatelliteEntity> Satellites => Set<SatelliteEntity>();
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<ImageDataEntity> ImageData => Set<ImageDataEntity>();
        public DbSet<OverpassEntity> Overpasses => Set<OverpassEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ====================================================================================
            // Entity Schema Configurations
            // ====================================================================================

            #region Core Entities (Users, Satellites, Ground Stations)

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

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Role);
            });

            modelBuilder.Entity<SatelliteEntity>(entity =>
            {
                entity.ToTable("satellites");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.NoradId).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.LastUpdate).HasDefaultValueSql("timezone('utc', now())");

                entity.HasIndex(e => e.NoradId).IsUnique();
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<GroundStationEntity>(entity =>
            {
                entity.ToTable("ground_stations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");

                entity.HasIndex(e => e.ApplicationId).IsUnique();

                entity.OwnsOne(e => e.Location, location =>
                {
                    location.Property(l => l.Latitude).IsRequired().HasColumnName("latitude");
                    location.Property(l => l.Longitude).IsRequired().HasColumnName("longitude");
                    location.Property(l => l.Altitude).HasColumnName("altitude").HasDefaultValue(0);
                });
            });

            #endregion

            #region Relational & Data Entities (FlightPlans, Overpasses, etc.)

            modelBuilder.Entity<FlightPlanEntity>(entity =>
            {
                entity.ToTable("flight_plans");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("timezone('utc', now())");

                if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
                {
                    entity.Property(e => e.Commands).HasColumnType("jsonb").IsRequired();
                }
                else
                {
                    entity.Property(e => e.Commands)
                        .HasConversion(v => v.RootElement.GetRawText(), v => JsonDocument.Parse(v, new JsonDocumentOptions()))
                        .HasColumnType("text").IsRequired();
                }

                entity.HasIndex(e => e.Status);

                // --- Relationships ---
                entity.HasOne(fp => fp.GroundStation)
                    .WithMany(gs => gs.FlightPlans)
                    .HasForeignKey(fp => fp.GroundStationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fp => fp.Satellite)
                    .WithMany(s => s.FlightPlans)
                    .HasForeignKey(fp => fp.SatelliteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fp => fp.CreatedBy)
                    .WithMany(u => u.CreatedFlightPlans)
                    .HasForeignKey(fp => fp.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fp => fp.ApprovedBy)
                    .WithMany(u => u.ApprovedFlightPlans)
                    .HasForeignKey(fp => fp.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(fp => fp.PreviousPlan)
                    .WithMany()
                    .HasForeignKey(fp => fp.PreviousPlanId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<OverpassEntity>(entity =>
            {
                entity.ToTable("overpasses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn();

                // --- Relationships ---
                entity.HasOne(e => e.Satellite)
                    .WithMany(s => s.Overpasses)
                    .HasForeignKey(e => e.SatelliteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.GroundStation)
                    .WithMany(gs => gs.Overpasses)
                    .HasForeignKey(e => e.GroundStationId)
                    .OnDelete(DeleteBehavior.Cascade);

                // --- Indexes ---
                entity.HasIndex(e => new { e.SatelliteId, e.GroundStationId, e.StartTime });
            });

            modelBuilder.Entity<ImageDataEntity>(entity =>
            {
                entity.ToTable("image_data");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityByDefaultColumn();
                entity.Property(e => e.S3ObjectPath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ReceivedAt).HasDefaultValueSql("timezone('utc', now())");
                entity.Property(e => e.Latitude).HasPrecision(9, 6);
                entity.Property(e => e.Longitude).HasPrecision(9, 6);
                entity.Property(e => e.Metadata).HasColumnType("jsonb");

                // --- Relationships ---
                // Images are dependent data. Cascade deletes are appropriate.
                entity.HasOne(id => id.Satellite)
                    .WithMany(s => s.Images)
                    .HasForeignKey(id => id.SatelliteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(id => id.GroundStation)
                    .WithMany(gs => gs.Images)
                    .HasForeignKey(id => id.GroundStationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(id => id.FlightPlan)
                    .WithMany()
                    .HasForeignKey(id => id.FlightPlanId)
                    .OnDelete(DeleteBehavior.SetNull);

                // --- Indexes ---
                entity.HasIndex(e => new { e.Latitude, e.Longitude });
                entity.HasIndex(e => e.CaptureTime);
                entity.HasIndex(e => e.FlightPlanId);
            });

            #endregion

            // ====================================================================================
            // Seeding Data
            // ====================================================================================

            // NOTE: Default admin user removed - users are now managed via Auth0
            // Admins should be assigned the Admin role through the user management API after first login

            modelBuilder.Entity<GroundStationEntity>().HasData(
                new GroundStationEntity
                {
                    Id = 1,
                    Name = "Aarhus",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<GroundStationEntity>()
                .OwnsOne(e => e.Location)
                .HasData(new
                {
                    GroundStationId = 1,
                    Latitude = 56.17197289799066,
                    Longitude = 10.191659216036516,
                    Altitude = 62.0
                });

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