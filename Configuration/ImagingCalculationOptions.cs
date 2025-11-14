namespace SatOps.Configuration
{
    /// <summary>
    /// Configuration options for imaging opportunity calculations.
    /// These values control how the system searches for optimal imaging times.
    /// </summary>
    public class ImagingCalculationOptions
    {
        /// <summary>
        /// Maximum duration to search ahead for imaging opportunities (in hours).
        /// Default: 48 hours
        /// </summary>
        public int MaxSearchDurationHours { get; set; } = 48;

        /// <summary>
        /// Maximum acceptable off-nadir angle for imaging (in degrees).
        /// Lower values mean stricter requirements for imaging opportunities.
        /// Default: 80.0 degrees
        /// </summary>
        public double MaxOffNadirDegrees { get; set; } = 40.0;
    }
}
