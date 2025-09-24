

// Overpass entity model for a satellite

// Potential useful libraries here:
// https://github.com/1manprojects/one_Sgp4
// https://github.com/parzivail/SGP.NET


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

//TODO: Make sure sceduler uses this entity to store overpass data and handle cascated delete

//TODO: Add Tle update time so it is possible to know if it was old tle data
namespace SatOps.Modules.Overpass
{
    [Table("overpasses")]
    public class Entity
    {
        public int Id { get; set; }
        public int SatelliteId { get; set; }
        public int GroundStationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime MaxElevationTime { get; set; }
        public double MaxElevation { get; set; }
        public int DurationSeconds { get; set; }
        public double StartAzimuth { get; set; }
        public double EndAzimuth { get; set; }
    }
}