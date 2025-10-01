namespace SatOps.Modules.Operation
{
    public class TelemetryData
    {
        public int Id { get; set; }
        public int GroundStationId { get; set; }
        public int SatelliteId { get; set; }
        public int FlightPlanId { get; set; }
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
        public string? Metadata { get; set; } // JSON metadata about the image
    }
}
