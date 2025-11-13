using Xunit;
using FluentAssertions;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.FlightPlan.Commands;
using System.ComponentModel.DataAnnotations;

namespace SatOps.Tests.FlightPlan
{
    public class CommandValidationTests
    {
        private (bool IsValid, List<ValidationResult> Errors) ValidateCommand(Command command)
        {
            var context = new ValidationContext(command);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);
            return (isValid, results);
        }

        #region TriggerCaptureCommand Tests

        [Fact]
        public void TriggerCapture_Validate_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                CaptureLocation = new CaptureLocation { Latitude = 50, Longitude = 10 },
                CameraSettings = new CameraSettings
                {
                    CameraId = "TestCam",
                    Type = CameraType.Test,
                    ExposureMicroseconds = 50000,
                    Iso = 1.0,
                    NumImages = 1,
                    IntervalMicroseconds = 0,
                    ObservationId = 1,
                    PipelineId = 1
                },
                ExecutionTime = null
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Fact]
        public void TriggerCapture_Validate_WithExecutionTimeProvided_ReturnsError()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                CaptureLocation = new CaptureLocation { Latitude = 50, Longitude = 10 },
                CameraSettings = new CameraSettings { CameraId = "TestCam", Type = CameraType.Test, ExposureMicroseconds = 50000, Iso = 1.0, NumImages = 1, IntervalMicroseconds = 0, ObservationId = 1, PipelineId = 1 },
                ExecutionTime = DateTime.UtcNow
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().ContainSingle(e => e.ErrorMessage!.Contains("ExecutionTime should not be provided"));
        }

        [Fact]
        public void TriggerCapture_Validate_WithNullCaptureLocation_ReturnsError()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                CaptureLocation = null, // Invalid
                CameraSettings = new CameraSettings { CameraId = "TestCam", Type = CameraType.Test, ExposureMicroseconds = 50000, Iso = 1.0, NumImages = 1, IntervalMicroseconds = 0, ObservationId = 1, PipelineId = 1 }
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().ContainSingle(e => e.ErrorMessage!.Contains("CaptureLocation is required"));
        }

        [Fact]
        public void TriggerCapture_Validate_WithInvalidCameraSettings_ReturnsError()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                CaptureLocation = new CaptureLocation { Latitude = 50, Longitude = 10 },
                CameraSettings = new CameraSettings
                {
                    CameraId = "TestCam",
                    Type = CameraType.Test,
                    ExposureMicroseconds = 50000,
                    NumImages = 0, // Invalid value
                    Iso = 99, // Invalid value
                    IntervalMicroseconds = 1000,
                    ObservationId = 1,
                    PipelineId = 1
                }
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().HaveCount(2);
            errors.Select(e => e.ErrorMessage).Should().Contain("NumImages must be between 1 and 1000");
            errors.Select(e => e.ErrorMessage).Should().Contain("Iso must be between 0.1 and 10.0");
        }

        [Fact]
        public void TriggerCapture_Validate_WithMultipleImagesAndZeroInterval_ReturnsError()
        {
            // Arrange
            var command = new TriggerCaptureCommand
            {
                CaptureLocation = new CaptureLocation { Latitude = 50, Longitude = 10 },
                CameraSettings = new CameraSettings
                {
                    CameraId = "TestCam",
                    Type = CameraType.Test,
                    ExposureMicroseconds = 50000,
                    Iso = 1.0,
                    NumImages = 5, // Multiple images
                    IntervalMicroseconds = 0, // Invalid interval
                    ObservationId = 1,
                    PipelineId = 1
                }
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().ContainSingle(e => e.ErrorMessage!.Contains("IntervalMicroseconds must be greater than 0"));
        }

        #endregion

        #region TriggerPipelineCommand Tests

        [Fact]
        public void TriggerPipeline_Validate_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var command = new TriggerPipelineCommand
            {
                Mode = 1,
                ExecutionTime = DateTime.UtcNow
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Fact]
        public void TriggerPipeline_Validate_WithNullExecutionTime_ReturnsError()
        {
            // Arrange
            var command = new TriggerPipelineCommand
            {
                Mode = 1,
                ExecutionTime = null // Invalid
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().ContainSingle(e => e.ErrorMessage == "ExecutionTime is required");
        }

        [Fact]
        public void TriggerPipeline_Validate_WithNullMode_ReturnsError()
        {
            // Arrange
            var command = new TriggerPipelineCommand
            {
                Mode = null, // Invalid
                ExecutionTime = DateTime.UtcNow
            };

            // Act
            var (isValid, errors) = ValidateCommand(command);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().ContainSingle(e => e.ErrorMessage == "Mode is required");
        }

        #endregion
    }
}