# SatOps API

A backend API for satellite operations, designed to manage ground stations, track satellites, calculate overpasses, and schedule flight plans.

## Features

- **Ground Station Management:** Full CRUD for ground stations with automated, periodic health checks via a background worker.
- **Satellite Tracking:** Manages a catalog of satellites and automatically updates TLE data from Celestrack.
- **Overpass Calculation:** Predicts satellite visibility windows for any ground station.
- **Flight Plan Scheduling:** Create, update (with versioning), and manage the approval lifecycle of flight plans.
- **Object Storage:** Integrates with MinIO for scalable storage of large binary data like satellite imagery.
- **Containerized:** Ready to run with Docker and Docker Compose.

## Technology Stack

- **.NET 8** / ASP.NET Core
- **Entity Framework Core**
- **PostgreSQL** + **PostGIS** for relational and spatial data.
- **Minio** for S3-compatible object storage.
- **Docker** for containerization.
- **Google.Protobuf** for parsing satellite binary containers.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Running the Application

1.  **Clone the repository:**

    ```bash
    git clone https://github.com/your-username/dietzy1-satops.git
    cd dietzy1-satops
    ```

2.  **Run with Docker Compose:**
    This command will build the API image, start the PostgreSQL database, apply migrations, and run the application.

    ```bash
    docker-compose up --build
    ```

3.  **Access the API:**
    The SatOps API (Swagger) will be available at `http://localhost:5111/swagger`.

## Satellite Command Architecture (For Developers & Ops)

The flight planning system uses a polymorphic command pattern to generate CSH (Cubesat Space Protocol Shell) scripts.

### Extending with New Commands

As the satellite capabilities evolve (e.g., adding `ConfigureModule` for AI tuning or `Reboot`), you will need to implement new command types.

1.  **Define the Command Class:**
    Create a new class in `Modules/FlightPlan/Commands/` inheriting from `Command`.

    ```csharp
    public class RebootCommand : Command
    {
        public override string CommandType => "REBOOT_SYSTEM";
        public override Task<List<string>> CompileToCsh()
        {
            return Task.FromResult(new List<string> { $"cmp reboot -n {NodeId}" });
        }
    }
    ```

2.  **Register the Type:**
    Update `CommandTypeConstants` in `Command.cs` with your new string discriminator.

3.  **Update Serialization Logic:**
    You **must** add the new case to the `CommandJsonConverter` class in `Command.cs` so the API can deserialize incoming JSON payloads into your new class.

    ```csharp
    return commandType switch
    {
        CommandTypeConstants.TriggerCapture => ...,
        CommandTypeConstants.RebootSystem => JsonSerializer.Deserialize<RebootCommand>(...),
        _ => throw new JsonException(...)
    };
    ```

## Configuration & Known Limitations

### CSP Node Addresses

The satellite subsystems communicate via CSP (Cubesat Space Protocol). The Node IDs are currently **hardcoded** within the Command classes:

- **Camera Controller (DCC):** Node `2` (See `TriggerCaptureCommand.cs`)
- **Image Processing Pipeline (DIPP):** Node `162` (See `TriggerPipelineCommand.cs`)

**⚠️ Ops Note:** These node IDs may be incorrect or change.

## API Structure & Endpoints

The platform exposes two distinct APIs, documented via separate Swagger UIs.

#### Public API (`/api/v1/...`)

Intended for human operators and external management tools. Authentication is handled via local JWTs.

- `GET /api/v1/ground-stations` - List all ground stations.
- `POST /api/v1/ground-stations` - Create a new ground station.
- `GET /api/v1/ground-stations/{id}` - Get details for a specific ground station.
- `GET /api/v1/satellites` - List available satellites.
- `GET /api/v1/overpasses/satellite/{satId}/groundstation/{gsId}` - Calculate satellite overpasses.
- `CRUD /api/v1/flight-plans` - Manage the flight plan lifecycle.

#### Internal API (`/api/internal/...` & `/api/auth/...`)

A machine-to-machine API for ground stations. It is protected and requires a ground-station-specific JWT.

- `POST /api/auth/token` - Acquire a JWT using the ground station's Application ID & API Key.
- `POST /api/internal/operations/images` - Upload captured image files from a ground station.

## Logging & Monitoring

This project is configured with [Serilog](https://serilog.net/) for powerful structured logging.

When running with Docker Compose, all logs from the `satops-api` service are automatically sent to a [Seq](https://datalust.co/seq) container. This provides a rich, searchable interface for viewing and analyzing log data.

- **Seq UI:** Available at `http://localhost:5341`
