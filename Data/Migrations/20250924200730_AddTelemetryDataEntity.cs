using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryDataEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telemetry_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    FlightPlanId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt", "altitude", "latitude", "longitude" },
                values: new object[] { new DateTime(2025, 9, 24, 20, 7, 29, 834, DateTimeKind.Utc).AddTicks(8000), new DateTime(2025, 9, 24, 20, 7, 29, 834, DateTimeKind.Utc).AddTicks(8000), 62.0, 56.171972897990663, 10.191659216036516 });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 9, 24, 20, 7, 29, 835, DateTimeKind.Utc).AddTicks(480), new DateTime(2025, 9, 24, 20, 7, 29, 835, DateTimeKind.Utc).AddTicks(480) });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry_data");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt", "altitude", "latitude", "longitude" },
                values: new object[] { new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(4150), new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(4150), 0.0, 56.1629, 10.203900000000001 });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(5860), new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(5860) });
        }
    }
}
