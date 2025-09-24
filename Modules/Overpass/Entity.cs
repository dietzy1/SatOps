

// Overpass entity model for a satellite

// Potential useful libraries here:
// https://github.com/1manprojects/one_Sgp4
// https://github.com/parzivail/SGP.NET


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatOps.Overpass
{
    [Table("overpasses")]
    public class Entity
    {
        public int Id { get; set; }
        public int SatelliteId { get; set; }
        public int GroundStationId { get; set; }

        public int flightPlanID { get; set; }

        //TODO: two solutions big jsonb column with all data dumped into or just add seperate columns Terkel will figure out
        public string? OverpassData { get; set; }

    }
}