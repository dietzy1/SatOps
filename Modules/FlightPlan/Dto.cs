namespace SatOps.Modules.FlightPlan
{
    // We need to handle the case of being associated with an overpass and not being approved in time
    // Perhabs we need to have it so an aprove action is done with an association to an overpass
    public class FlightPlanDto
    {
        public int Id { get; set; }
        public string? PreviousPlanId { get; set; }
        public int GsId { get; set; }
        public int SatId { get; set; }
        public int? OverpassId { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string? ApproverId { get; set; }
        public DateTime? ApprovalDate { get; set; }
    }


    public class FlightPlanBodyDto
    {
        public string Name { get; set; } = string.Empty;

        public object Body { get; set; } = new();
    }

    // DTO for the POST request body
    public class CreateFlightPlanDto
    {
        public int GsId { get; set; }

        public int SatId { get; set; }

        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();
    }

    // DTO for the PATCH (approve/reject) request body
    public class ApproveFlightPlanDto
    {
        public string Status { get; set; } = string.Empty;
    }

    // DTO for associating with overpass using timerange and optional matching criteria
    public class AssociateOverpassDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Optional: Maximum elevation for more precise matching (in degrees)
        public double? MaxElevation { get; set; }

        // Optional: Duration in seconds for additional validation
        public int? DurationSeconds { get; set; }

        // Optional: Maximum elevation time for exact overpass identification
        public DateTime? MaxElevationTime { get; set; }
    }

    public class ImagingTimingRequestDto
    {
        public int SatelliteId { get; set; }
        public DateTime? CommandReceptionTime { get; set; }
        public double TargetLatitude { get; set; }
        public double TargetLongitude { get; set; }

        // Off-nadir imaging parameters (replaces fixed MaxDistanceKm)
        public double MaxOffNadirDegrees { get; set; } = 10.0; // Default 10° off-nadir maximum

        // Search parameters
        public int MaxSearchDurationHours { get; set; } = 48; // Search up to 48 hours ahead
    }

    public class ImagingTimingResponseDto
    {
        public DateTime? ImagingTime { get; set; }
        public double? OffNadirDegrees { get; set; }
        public double? SatelliteAltitudeKm { get; set; }

        // TLE age warning
        public bool TleAgeWarning { get; set; }
        public double? TleAgeHours { get; set; }

        public string? Message { get; set; }
    }
}