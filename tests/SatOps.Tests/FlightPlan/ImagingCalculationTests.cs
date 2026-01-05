using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using SatOps.Modules.FlightPlan;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.TLE;
using SGPdotNET.Propagation;

namespace SatOps.Tests.FlightPlan
{
    public class ImagingCalculationTests
    {
        private readonly ImagingCalculation _sut;
        private readonly Mock<ILogger<ImagingCalculation>> _loggerMock;

        // A static, frozen TLE for the ISS allows for deterministic testing.
        // Epoch: 2023-09-13 12:00:00 UTC
        private const string TleLine1 = "1 25544U 98067A   23256.50000000  .00016717  00000+0  10270-3 0  9002";
        private const string TleLine2 = "2 25544  51.6416 118.9708 0004523 127.3888 322.4932 15.50202292415516";
        private readonly DateTime _epoch;
        private readonly SGPdotNET.Observation.Satellite _satellite;

        public ImagingCalculationTests()
        {
            _loggerMock = new Mock<ILogger<ImagingCalculation>>();
            _sut = new ImagingCalculation(_loggerMock.Object);

            var tle = new Tle("ISS", TleLine1, TleLine2);
            _satellite = new SGPdotNET.Observation.Satellite(tle);
            _epoch = tle.Epoch;
        }

        [Fact]
        public void FindBestImagingOpportunity_WhenTargetIsDirectlyBeneathSatellite_ReturnsZeroOffNadir()
        {
            // Arrange
            // Predict where the satellite is exactly 10 minutes after epoch
            var targetTime = _epoch.AddMinutes(10);
            var satPosEci = _satellite.Predict(targetTime);
            var satPosGeo = satPosEci.ToGeodetic();

            // Place our target exactly at that lat/lon on the ground
            var target = new GeodeticCoordinate(satPosGeo.Latitude, satPosGeo.Longitude, 0);

            // Search in a window that encompasses this time
            var searchStart = targetTime.AddMinutes(-5);
            var duration = TimeSpan.FromMinutes(10);

            // Act
            var result = _sut.FindBestImagingOpportunity(_satellite, target, searchStart, duration);

            // Assert
            result.Should().NotBeNull();

            // The algorithm should converge exactly on the time we predicted
            result!.ImagingTime.Should().BeCloseTo(targetTime, precision: TimeSpan.FromSeconds(0.5));

            // Math Check: If target is directly underneath, Off-Nadir should be ~0
            result.OffNadirDegrees.Should().BeApproximately(0, 0.1);

            // Math Check: Distance should be ~0
            result.DistanceKm.Should().BeApproximately(0, 1.0); // Within 1km

            // Math Check: Slant Range should equal Satellite Altitude (approx 415-420km for ISS)
            result.SlantRangeKm.Should().BeApproximately(satPosGeo.Altitude, 1.0);
            result.SatelliteAltitudeKm.Should().BeApproximately(satPosGeo.Altitude, 0.01);
        }

        [Fact]
        public void FindBestImagingOpportunity_WhenTargetIsOffsetByKnownDistance_ReturnsConsistentGeometry()
        {
            // Arrange
            var targetTime = _epoch.AddMinutes(10);
            var satPosEci = _satellite.Predict(targetTime);
            var satPosGeo = satPosEci.ToGeodetic();

            // Place target 1 degree latitude North of the satellite.
            // Note: The satellite is moving on an inclined orbit (51.6°). Even though we place
            // the target 1° North of the *snapshot* position, the satellite's path might
            // bring it closer than 111km (or further) at the optimal imaging time.
            var targetLat = satPosGeo.Latitude.Degrees + 1.0;
            var target = new GeodeticCoordinate(
                SGPdotNET.Util.Angle.FromDegrees(targetLat),
                satPosGeo.Longitude,
                0);

            var searchStart = targetTime.AddMinutes(-2);
            var duration = TimeSpan.FromMinutes(4);

            // Act
            var result = _sut.FindBestImagingOpportunity(_satellite, target, searchStart, duration);

            // Assert
            result.Should().NotBeNull();

            // Verify reasonable distance. 1 deg lat is ~111km. 
            // The optimization algorithm will find the "Cross Track" distance, which 
            // should be <= the snapshot distance.
            result!.DistanceKm.Should().BeGreaterThan(20);
            result.DistanceKm.Should().BeLessThan(120);

            // Verify Geometric Consistency (Law of Sines check)
            // Formula: Re / sin(eta) = d / sin(theta)
            // Cross multiply: Re * sin(theta) = d * sin(eta)
            // Re = Earth Radius, d = Slant Range
            // theta = Central Angle (Ground distance in radians)
            // eta = Off-Nadir Angle

            double Re = SgpConstants.EarthRadiusKm;
            double groundAngleRad = result.DistanceKm / Re; // theta
            double offNadirRad = result.OffNadirDegrees * Math.PI / 180.0; // eta

            double lhs = Re * Math.Sin(groundAngleRad);          // Re * sin(theta)
            double rhs = result.SlantRangeKm * Math.Sin(offNadirRad); // d * sin(eta)

            // Allow small epsilon for floating point variance and iterative precision
            lhs.Should().BeApproximately(rhs, 1.0,
                $"Law of Sines must hold: Re*sin(Ground)={lhs:F2} vs Slant*sin(OffNadir)={rhs:F2}");

            // Verify Slant Range is logical (Hypotenuse > Altitude)
            result.SlantRangeKm.Should().BeGreaterThan(result.SatelliteAltitudeKm);
        }

        [Fact]
        public void FindBestImagingOpportunity_WhenSatelliteNeverRises_ReturnsNull()
        {
            // Arrange
            // Predict where sat is
            var satPosEci = _satellite.Predict(_epoch);
            var satPosGeo = satPosEci.ToGeodetic();

            // Place target on the EXACT OPPOSITE side of the Earth (Antipode)
            // If sat is at Lat 51, Target at -51. If Lon is 118, Target is 118-180.
            var targetLat = -satPosGeo.Latitude.Degrees;
            var targetLon = satPosGeo.Longitude.Degrees > 0
                ? satPosGeo.Longitude.Degrees - 180
                : satPosGeo.Longitude.Degrees + 180;

            var target = new GeodeticCoordinate(
                SGPdotNET.Util.Angle.FromDegrees(targetLat),
                SGPdotNET.Util.Angle.FromDegrees(targetLon),
                0);

            // Search a small window where we know the sat is on the other side
            var duration = TimeSpan.FromMinutes(10);

            // Act
            var result = _sut.FindBestImagingOpportunity(_satellite, target, _epoch, duration);

            // Assert
            result.Should().BeNull("Satellite is on opposite side of Earth (occluded)");
        }

        [Fact]
        public void FindBestImagingOpportunity_SearchConvergence_FindsPeakWithinWindow()
        {
            // Arrange
            // We want to verify the algorithm iterates (Coarse -> Fine -> Finest).
            // We pick a time where the pass happens exactly in the middle of a 10 minute window.
            var peakTime = _epoch.AddMinutes(30);
            var satPos = _satellite.Predict(peakTime).ToGeodetic();
            var target = new GeodeticCoordinate(satPos.Latitude, satPos.Longitude, 0);

            // Start searching 5 minutes BEFORE the peak
            var searchStart = peakTime.AddMinutes(-5);
            var duration = TimeSpan.FromMinutes(10);

            // Act
            var result = _sut.FindBestImagingOpportunity(_satellite, target, searchStart, duration);

            // Assert
            result.Should().NotBeNull();
            // The logic implies: Coarse steps (120s) -> Find window -> Refine (2s) -> Refine (0.1s).
            // The result should be extremely close to the actual moment the satellite is overhead.
            result!.ImagingTime.Should().BeCloseTo(peakTime, precision: TimeSpan.FromSeconds(0.2));
            result.OffNadirDegrees.Should().BeLessThan(0.1);
        }
    }
}