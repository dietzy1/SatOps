using Xunit;
using FluentAssertions;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.FlightPlan.Commands;
using System;
using System.Threading.Tasks;

namespace SatOps.Tests.FlightPlan
{
    public class CommandCompilationTests
    {
        [Fact]
        public async Task TriggerCaptureCommand_CompileToCsh_GeneratesCorrectSingleStringCommand()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                // This must be set, as your method validates it.
                ExecutionTime = DateTime.UtcNow,
                CaptureLocation = new CaptureLocation { Latitude = 55.6, Longitude = 12.5 },
                CameraSettings = new CameraSettings
                {
                    CameraId = "TestCam-123",
                    Type = CameraType.VMB,
                    ExposureMicroseconds = 50000,
                    Iso = 1.5,
                    NumImages = 5,
                    IntervalMicroseconds = 10000,
                    ObservationId = 99,
                    PipelineId = 101
                }
            };

            var expectedPayload = "CAMERA_ID=TestCam-123;CAMERA_TYPE=VMB;NUM_IMAGES=5;EXPOSURE=50000;ISO=1.5;INTERVAL=10000;OBID=99;PIPELINE_ID=101;";
            var expectedCsh = $"set capture_param \"{expectedPayload}\" -n 2";

            // Act
            var result = await command.CompileToCsh();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Should().Be(expectedCsh);
        }

        [Fact]
        public async Task TriggerPipelineCommand_CompileToCsh_GeneratesCorrectSetCommand()
        {
            // Arrange
            var command = new TriggerPipelineCommand
            {
                ExecutionTime = DateTime.UtcNow,
                Mode = 2
            };
            var expectedCsh = "set pipeline_run 2 -n 162";

            // Act
            var result = await command.CompileToCsh();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Should().Be(expectedCsh);
        }

        [Fact]
        public void TriggerCaptureCommand_CompileToCsh_WhenExecutionTimeIsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                ExecutionTime = null, // Invalid state for compilation
                CaptureLocation = new CaptureLocation(),
                CameraSettings = new CameraSettings() // Needs to be initialized
                {
                    CameraId = "any",
                    Type = CameraType.Test,
                    NumImages = 1,
                    ExposureMicroseconds = 1,
                    IntervalMicroseconds = 1,
                    Iso = 1,
                    ObservationId = 1,
                    PipelineId = 1
                }
            };

            // Act
            Func<Task> act = () => command.CompileToCsh();

            // Assert
            act.Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*ExecutionTime must be calculated before compiling*");
        }
    }
}