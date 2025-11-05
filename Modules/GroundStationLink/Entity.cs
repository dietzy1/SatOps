using System.ComponentModel.DataAnnotations.Schema;
using SatOps.Modules.Groundstation;

namespace SatOps.Modules.GroundStationLink
{
    public class TelemetryData
    {
        public int Id { get; set; }
        public int GroundStationId { get; set; }
        public int SatelliteId { get; set; }
        public int FlightPlanId { get; set; }

        [ForeignKey(nameof(GroundStationId))]
        public virtual GroundStation GroundStation { get; set; } = null!;
        [ForeignKey(nameof(SatelliteId))]
        public virtual Satellite.Satellite Satellite { get; set; } = null!;
        [ForeignKey(nameof(FlightPlanId))]
        public virtual FlightPlan.FlightPlan FlightPlan { get; set; } = null!;

        public DateTime Timestamp { get; set; }
        public string S3ObjectPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }

    public class ImageData
    {
        public int Id { get; set; }
        public int SatelliteId { get; set; }
        public int GroundStationId { get; set; }

        [ForeignKey(nameof(SatelliteId))]
        public virtual Satellite.Satellite Satellite { get; set; } = null!;
        [ForeignKey(nameof(GroundStationId))]
        public virtual GroundStation GroundStation { get; set; } = null!;

        public DateTime CaptureTime { get; set; }
        public string S3ObjectPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public string? Metadata { get; set; }
    }
}