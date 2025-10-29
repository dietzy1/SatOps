namespace SatOps.Authorization
{
    /// <summary>
    /// Defines all authorization scopes used in the SatOps system.
    /// </summary>
    public static class Scopes
    {

        public const string ReadGroundStations = "read:ground-stations";
        public const string WriteGroundStations = "write:ground-stations";

        public const string ReadSatellites = "read:satellites";
        public const string WriteSatellites = "write:satellites";

        public const string ReadFlightPlans = "read:flight-plans";
        public const string WriteFlightPlans = "write:flight-plans";

        public const string ManageUsers = "manage:users";

        public const string UploadTelemetry = "upload:telemetry";
        public const string UploadImages = "upload:images";
        public const string EstablishWebSocket = "establish:websocket";
    }

    /// <summary>
    /// Defines policy names for authorization.
    /// </summary>
    public static class Policies
    {
        public const string ReadGroundStations = "ReadGroundStations";
        public const string WriteGroundStations = "WriteGroundStations";

        public const string ReadSatellites = "ReadSatellites";
        public const string WriteSatellites = "WriteSatellites";

        public const string ReadFlightPlans = "ReadFlightPlans";
        public const string WriteFlightPlans = "WriteFlightPlans";

        public const string ManageUsers = "ManageUsers";

        public const string UploadTelemetry = "UploadTelemetry";
        public const string UploadImages = "UploadImages";
        public const string EstablishWebSocket = "EstablishWebSocket";

        public const string RequireGroundStation = "RequireGroundStation";
        public const string RequireAdmin = "RequireAdmin";
    }
}
