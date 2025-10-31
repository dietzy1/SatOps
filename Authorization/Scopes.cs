namespace SatOps.Authorization
{
    /// <summary>
    /// Defines policy names for authorization.
    /// Role-based policies map to user roles: Viewer, Operator, Admin.
    /// Ground station policies are for machine-to-machine authentication.
    /// </summary>
    public static class Policies
    {
        // Role-based policies for human users
        // These check if the user has the minimum required role
        public const string RequireViewer = "RequireViewer";      // Viewer, Operator, or Admin
        public const string RequireOperator = "RequireOperator";  // Operator or Admin
        public const string RequireAdmin = "RequireAdmin";        // Admin only

        // Special policy for ground station machine authentication
        public const string RequireGroundStation = "RequireGroundStation";
    }

    /// <summary>
    /// User roles in the system.
    /// Matches the UserRole enum but as strings for authorization.
    /// </summary>
    public static class Roles
    {
        public const string Viewer = "Viewer";
        public const string Operator = "Operator";
        public const string Admin = "Admin";
    }

    /// <summary>
    /// Scopes for ground station machine-to-machine authentication.
    /// Ground stations use these scopes instead of roles.
    /// </summary>
    public static class GroundStationScopes
    {
        public const string UploadTelemetry = "gs:upload-telemetry";
        public const string UploadImages = "gs:upload-images";
        public const string EstablishWebSocket = "gs:establish-websocket";
    }
}
