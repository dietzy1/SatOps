# SatOps API

A backend API for satellite operations, designed to manage ground stations, track satellites, calculate overpasses, and schedule flight plans.

## Features

- **Ground Station Management:** Full CRUD operations and automated health checks for ground stations.
- **Satellite Tracking:** List and retrieve satellite details, including TLE data.
- **Overpass Calculation:** Predict satellite overpasses for specific ground stations within a given time window.
- **Flight Plan Scheduling:** Create, update (with versioning), and approve/reject flight plans.
- **Health Monitoring:** A dedicated endpoint (`/api/v1/health`) for API status.
- **Containerized:** Ready to run with Docker and Docker Compose.

## Technology Stack

- **.NET 8** / ASP.NET Core
- **Entity Framework Core**
- **PostgreSQL** + **PostGIS**
- **Docker**

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
    The API will be available at `http://localhost:7890`.

## API Endpoints

A brief overview of the main endpoints:

| Method | Path                                                        | Description                  |
| :----- | :---------------------------------------------------------- | :--------------------------- |
| `GET`  | `/api/v1/health`                                            | Check the health of the API. |
| `CRUD` | `/api/v1/ground-stations`                                   | Manage ground stations.      |
| `GET`  | `/api/v1/satellites`                                        | List available satellites.   |
| `GET`  | `/api/v1/overpasses/satellite/{satId}/groundstation/{gsId}` | Calculate overpasses.        |
| `CRUD` | `/api/v1/flight-plans`                                      | Manage flight plans.         |
