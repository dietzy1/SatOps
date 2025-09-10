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
    }
}


