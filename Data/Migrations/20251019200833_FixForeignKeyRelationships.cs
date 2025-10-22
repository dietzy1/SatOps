using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_image_data_ground_stations_GroundStationId1",
                table: "image_data");

            migrationBuilder.DropForeignKey(
                name: "FK_image_data_satellites_SatelliteId1",
                table: "image_data");

            migrationBuilder.DropForeignKey(
                name: "FK_overpasses_ground_stations_GroundStationId1",
                table: "overpasses");

            migrationBuilder.DropForeignKey(
                name: "FK_overpasses_satellites_SatelliteId1",
                table: "overpasses");

            migrationBuilder.DropForeignKey(
                name: "FK_telemetry_data_ground_stations_GroundStationId1",
                table: "telemetry_data");

            migrationBuilder.DropForeignKey(
                name: "FK_telemetry_data_satellites_SatelliteId1",
                table: "telemetry_data");

            migrationBuilder.DropIndex(
                name: "IX_telemetry_data_GroundStationId1",
                table: "telemetry_data");

            migrationBuilder.DropIndex(
                name: "IX_telemetry_data_SatelliteId1",
                table: "telemetry_data");

            migrationBuilder.DropIndex(
                name: "IX_overpasses_GroundStationId1",
                table: "overpasses");

            migrationBuilder.DropIndex(
                name: "IX_overpasses_SatelliteId1",
                table: "overpasses");

            migrationBuilder.DropIndex(
                name: "IX_image_data_GroundStationId1",
                table: "image_data");

            migrationBuilder.DropIndex(
                name: "IX_image_data_SatelliteId1",
                table: "image_data");

            migrationBuilder.DropColumn(
                name: "GroundStationId1",
                table: "telemetry_data");

            migrationBuilder.DropColumn(
                name: "SatelliteId1",
                table: "telemetry_data");

            migrationBuilder.DropColumn(
                name: "GroundStationId1",
                table: "overpasses");

            migrationBuilder.DropColumn(
                name: "SatelliteId1",
                table: "overpasses");

            migrationBuilder.DropColumn(
                name: "GroundStationId1",
                table: "image_data");

            migrationBuilder.DropColumn(
                name: "SatelliteId1",
                table: "image_data");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("c5cdbad5-95da-4cca-8f7d-c137fe79fe5a"), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(7971), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(7971) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8086), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8087) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8088), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8089) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroundStationId1",
                table: "telemetry_data",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SatelliteId1",
                table: "telemetry_data",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroundStationId1",
                table: "overpasses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SatelliteId1",
                table: "overpasses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroundStationId1",
                table: "image_data",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SatelliteId1",
                table: "image_data",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("7834a117-7061-4501-aa46-4b8e1330e0d2"), new DateTime(2025, 10, 17, 19, 33, 51, 553, DateTimeKind.Utc).AddTicks(9853), new DateTime(2025, 10, 17, 19, 33, 51, 553, DateTimeKind.Utc).AddTicks(9853) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 17, 19, 33, 51, 553, DateTimeKind.Utc).AddTicks(9968), new DateTime(2025, 10, 17, 19, 33, 51, 553, DateTimeKind.Utc).AddTicks(9968) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 17, 19, 33, 51, 553, DateTimeKind.Utc).AddTicks(9970), new DateTime(2025, 10, 17, 19, 33, 51, 553, DateTimeKind.Utc).AddTicks(9970) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_GroundStationId1",
                table: "telemetry_data",
                column: "GroundStationId1");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_data_SatelliteId1",
                table: "telemetry_data",
                column: "SatelliteId1");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_GroundStationId1",
                table: "overpasses",
                column: "GroundStationId1");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_SatelliteId1",
                table: "overpasses",
                column: "SatelliteId1");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_GroundStationId1",
                table: "image_data",
                column: "GroundStationId1");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_SatelliteId1",
                table: "image_data",
                column: "SatelliteId1");

            migrationBuilder.AddForeignKey(
                name: "FK_image_data_ground_stations_GroundStationId1",
                table: "image_data",
                column: "GroundStationId1",
                principalTable: "ground_stations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_image_data_satellites_SatelliteId1",
                table: "image_data",
                column: "SatelliteId1",
                principalTable: "satellites",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_overpasses_ground_stations_GroundStationId1",
                table: "overpasses",
                column: "GroundStationId1",
                principalTable: "ground_stations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_overpasses_satellites_SatelliteId1",
                table: "overpasses",
                column: "SatelliteId1",
                principalTable: "satellites",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_telemetry_data_ground_stations_GroundStationId1",
                table: "telemetry_data",
                column: "GroundStationId1",
                principalTable: "ground_stations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_telemetry_data_satellites_SatelliteId1",
                table: "telemetry_data",
                column: "SatelliteId1",
                principalTable: "satellites",
                principalColumn: "Id");
        }
    }
}
