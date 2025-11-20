using Xunit;
using FluentAssertions;
using SatOps.Modules.FlightPlan.Commands;

namespace SatOps.Tests.FlightPlan
{
    public class CommandCompilationTests
    {
        [Fact]
        public async Task TriggerCaptureCommand_CompileToCsh_GeneratesCorrectSequenceOfCommands()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
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

            // Act
            var result = await command.CompileToCsh();

            // Assert
            result.Should().NotBeNull();

            result.Count.Should().BeGreaterThan(5);

            // Verify specific lines are present in the correct order logic
            result.Should().Contain($"set camera_id_param \"TestCam-123\" -n 2");
            result.Should().Contain($"set camera_type_param 0 -n 2"); // Enum 0 = VMB
            result.Should().Contain($"set exposure_param 50000 -n 2");
            result.Should().Contain($"set iso_param 1.5 -n 2");
            result.Should().Contain($"set num_images_param 5 -n 2");
            result.Should().Contain($"set interval_param 10000 -n 2");
            result.Should().Contain($"set obid_param 99 -n 2");
            result.Should().Contain($"set pipeline_id_param 101 -n 2");

            // Verify the power-on sequence
            result.Should().Contain($"set camera_state_param 1 -n 2");
            result.Should().Contain("sleep 5");

            // Verify the trigger is the LAST command
            result.Last().Should().Be($"set capture_param 1 -n 2");
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

        [Fact]
        public async Task TriggerCaptureCommand_CompileToCsh_WithMaliciousCameraId_ThrowsInvalidOperationException()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                ExecutionTime = DateTime.UtcNow,
                CaptureLocation = new CaptureLocation { Latitude = 0, Longitude = 0 },
                CameraSettings = new CameraSettings
                {
                    CameraId = "\"; reboot; \"",
                    Type = CameraType.VMB,
                    ExposureMicroseconds = 100,
                    Iso = 1.0,
                    NumImages = 1,
                    IntervalMicroseconds = 100,
                    ObservationId = 1,
                    PipelineId = 1
                }
            };

            // Act
            Func<Task> act = () => command.CompileToCsh();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*Invalid Camera ID format*");
        }
    }
}