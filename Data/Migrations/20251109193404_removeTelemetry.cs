using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class removeTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry_data");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("a76767d0-a48e-44ed-ac86-19ccefb0ac71"), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3300), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3300) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telemetry_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlightPlanId = table.Column<int>(type: "integer", nullable: false),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    S3ObjectPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_telemetry_data_flight_plans_FlightPlanId",
                        column: x => x.FlightPlanId,
                        principalTable: "flight_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_telemetry_data_ground_stations_GroundStationId",
                        column: x => x.GroundStationId,
                        principalTable: "ground_stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_telemetry_data_satellites_SatelliteId",
                        column: x => x.SatelliteId,
                        principalTable: "satellites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("9d77315c-6b7c-4048-bbe3-3bfaa211a8d9"), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(6810), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(6810) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7020), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7020) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7030), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7030) });

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_FlightPlanId",
                table: "telemetry_data",
                column: "FlightPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_GroundStationId",
                table: "telemetry_data",
                column: "GroundStationId");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_SatelliteId_GroundStationId_Timestamp",
                table: "telemetry_data",
                columns: new[] { "SatelliteId", "GroundStationId", "Timestamp" });
        }
    }
}
