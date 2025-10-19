using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "flight_plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Commands = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    OverpassId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreviousPlanId = table.Column<int>(type: "integer", nullable: true),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<int>(type: "integer", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flight_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ground_stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    altitude = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    HttpUrl = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ground_stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "image_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    CaptureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    S3ObjectPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    Latitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: true),
                    ImageWidth = table.Column<int>(type: "integer", nullable: true),
                    ImageHeight = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "overpasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxElevationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxElevation = table.Column<double>(type: "double precision", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    StartAzimuth = table.Column<double>(type: "double precision", nullable: false),
                    EndAzimuth = table.Column<double>(type: "double precision", nullable: false),
                    TleLine1 = table.Column<string>(type: "text", nullable: true),
                    TleLine2 = table.Column<string>(type: "text", nullable: true),
                    TleUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overpasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "satellites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    NoradId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TleLine1 = table.Column<string>(type: "text", nullable: false),
                    TleLine2 = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_satellites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "telemetry_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    FlightPlanId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    S3ObjectPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    AdditionalScopes = table.Column<List<string>>(type: "jsonb", nullable: false),
                    AdditionalRoles = table.Column<List<string>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ground_stations",
                columns: new[] { "Id", "ApiKeyHash", "ApplicationId", "CreatedAt", "HttpUrl", "Name", "UpdatedAt", "altitude", "latitude", "longitude" },
                values: new object[] { 1, "", new Guid("f90c735c-012e-4426-8eba-f65f5a6f7339"), new DateTime(2025, 10, 12, 15, 53, 50, 801, DateTimeKind.Utc).AddTicks(1490), "http://aarhus-groundstation.example.com", "Aarhus", new DateTime(2025, 10, 12, 15, 53, 50, 801, DateTimeKind.Utc).AddTicks(1490), 62.0, 56.171972897990663, 10.191659216036516 });

            migrationBuilder.InsertData(
                table: "satellites",
                columns: new[] { "Id", "CreatedAt", "LastUpdate", "Name", "NoradId", "Status", "TleLine1", "TleLine2" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 10, 12, 15, 53, 50, 801, DateTimeKind.Utc).AddTicks(5770), new DateTime(2025, 10, 12, 15, 53, 50, 801, DateTimeKind.Utc).AddTicks(5770), "International Space Station (ISS)", 25544, 0, "1 25544U 98067A   23256.90616898  .00020137  00000-0  35438-3 0  9992", "2 25544  51.6416 339.0970 0003835  48.3825  73.2709 15.50030022414673" },
                    { 2, new DateTime(2025, 10, 12, 15, 53, 50, 801, DateTimeKind.Utc).AddTicks(5770), new DateTime(2025, 10, 12, 15, 53, 50, 801, DateTimeKind.Utc).AddTicks(5770), "SENTINEL-2C", 60989, 0, "1 60989U 24157A   25270.79510520  .00000303  00000-0  13232-3 0  9996", "2 60989  98.5675 344.4033 0001006  86.9003 273.2295 14.30815465 55465" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_Status",
                table: "flight_plans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ground_stations_ApplicationId",
                table: "ground_stations",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_image_data_CaptureTime",
                table: "image_data",
                column: "CaptureTime");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_GroundStationId",
                table: "image_data",
                column: "GroundStationId");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_Latitude_Longitude",
                table: "image_data",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_image_data_ReceivedAt",
                table: "image_data",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_SatelliteId",
                table: "image_data",
                column: "SatelliteId");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_EndTime",
                table: "overpasses",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_GroundStationId",
                table: "overpasses",
                column: "GroundStationId");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_SatelliteId",
                table: "overpasses",
                column: "SatelliteId");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_SatelliteId_GroundStationId_StartTime",
                table: "overpasses",
                columns: new[] { "SatelliteId", "GroundStationId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_StartTime",
                table: "overpasses",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_satellites_NoradId",
                table: "satellites",
                column: "NoradId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_satellites_Status",
                table: "satellites",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_FlightPlanId",
                table: "telemetry_data",
                column: "FlightPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_GroundStationId",
                table: "telemetry_data",
                column: "GroundStationId");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_ReceivedAt",
                table: "telemetry_data",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_SatelliteId",
                table: "telemetry_data",
                column: "SatelliteId");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_Timestamp",
                table: "telemetry_data",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Role",
                table: "users",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flight_plans");

            migrationBuilder.DropTable(
                name: "ground_stations");

            migrationBuilder.DropTable(
                name: "image_data");

            migrationBuilder.DropTable(
                name: "overpasses");

            migrationBuilder.DropTable(
                name: "satellites");

            migrationBuilder.DropTable(
                name: "telemetry_data");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
