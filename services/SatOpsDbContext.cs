using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace SatOps.Services
{
    public class SatOpsDbContext : DbContext
    {
        public SatOpsDbContext(DbContextOptions<SatOpsDbContext> options) : base(options)
        {
        }

        public DbSet<SatOps.Services.GroundStation.GroundStation> GroundStations => Set<SatOps.Services.GroundStation.GroundStation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure PostGIS extension is enabled and geometry types are configured
            modelBuilder.HasPostgresExtension("postgis");

            modelBuilder.Entity<SatOps.Services.GroundStation.GroundStation>(entity =>
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
        }
    }
}


