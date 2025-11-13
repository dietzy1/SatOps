using Xunit;
using FluentAssertions;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.FlightPlan.Commands;
using System.Text.Json;
namespace SatOps.Tests.FlightPlan
{
  public class CommandSerializationTests
  {
    [Fact]
    public void Deserialize_WithValidTriggerCaptureJson_ReturnsCorrectObject()
    {
      // Arrange
      var json = """
            [
              {
                "commandType": "TRIGGER_CAPTURE",
                "captureLocation": { "latitude": 55.6, "longitude": 12.5 },
                "cameraSettings": {
                  "cameraId": "TestCam",
                  "type": "TEST",
                  "exposureMicroseconds": 50000,
                  "iso": 1.5,
                  "numImages": 5,
                  "intervalMicroseconds": 10000,
                  "observationId": 1,
                  "pipelineId": 1
                }
              }
            ]
            """;

      // Act
      var commands = CommandExtensions.FromJson(json);

      // Assert
      commands.Should().HaveCount(1);
      var cmd = commands[0].Should().BeOfType<TriggerCaptureCommand>().Subject;
      cmd.CaptureLocation!.Latitude.Should().Be(55.6);
      cmd.CameraSettings!.NumImages.Should().Be(5);
    }

    [Fact]
    public void Deserialize_WithValidTriggerPipelineJson_ReturnsCorrectObject()
    {
      // Arrange
      var executionTime = DateTime.UtcNow;
      var json = string.Format(@"
            [
              {{
                ""commandType"": ""TRIGGER_PIPELINE"",
                ""executionTime"": ""{0}"",
                ""mode"": 1
              }}
            ]
            ", executionTime.ToString("O"));

      // Act
      var commands = CommandExtensions.FromJson(json);

      // Assert
      commands.Should().HaveCount(1);
      var cmd = commands[0].Should().BeOfType<TriggerPipelineCommand>().Subject;
      cmd.Mode.Should().Be(1);
      cmd.ExecutionTime.Should().BeCloseTo(executionTime, precision: TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void Deserialize_WithMissingCommandType_ThrowsArgumentException()
    {
      // Arrange
      var json = """[ { "mode": 1 } ]""";

      // Act
      Action act = () => CommandExtensions.FromJson(json);

      // Assert
      act.Should().Throw<ArgumentException>()
          .WithInnerException<JsonException>()
          .WithMessage("Missing required 'commandType' property");
    }

    [Fact]
    public void Deserialize_WithUnknownCommandType_ThrowsArgumentException()
    {
      // Arrange
      var json = """[ { "commandType": "DO_STUFF" } ]""";

      // Act
      Action act = () => CommandExtensions.FromJson(json);

      // Assert
      act.Should().Throw<ArgumentException>()
          .WithInnerException<JsonException>()
          .WithMessage("Unknown commandType 'DO_STUFF'.*");
    }

    [Fact]
    public void Serialize_ThenDeserialize_ProducesEquivalentObject()
    {
      // Arrange
      var originalCommand = new TriggerCaptureCommand
      {
        CaptureLocation = new CaptureLocation { Latitude = 1.23, Longitude = 4.56 },
        CameraSettings = new CameraSettings
        {
          CameraId = "RoundTripCam",
          Type = CameraType.IR,
          ExposureMicroseconds = 12345,
          Iso = 2.5,
          NumImages = 10,
          IntervalMicroseconds = 50000,
          ObservationId = 99,
          PipelineId = 101
        }
      };
      var commandList = new List<Command> { originalCommand };

      // Act
      var json = commandList.ToJson();
      var deserializedList = CommandExtensions.FromJson(json);
      var resultCommand = deserializedList.Should().ContainSingle().Subject;

      // Assert
      resultCommand.Should().BeEquivalentTo(originalCommand);
    }
  }
}