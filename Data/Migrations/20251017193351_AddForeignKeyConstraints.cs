using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_telemetry_data_ReceivedAt",
                table: "telemetry_data");

            migrationBuilder.DropIndex(
                name: "IX_telemetry_data_SatelliteId",
                table: "telemetry_data");

            migrationBuilder.DropIndex(
                name: "IX_telemetry_data_Timestamp",
                table: "telemetry_data");

            migrationBuilder.DropIndex(
                name: "IX_overpasses_EndTime",
                table: "overpasses");

            migrationBuilder.DropIndex(
                name: "IX_overpasses_SatelliteId",
                table: "overpasses");

            migrationBuilder.DropIndex(
                name: "IX_overpasses_StartTime",
                table: "overpasses");

            migrationBuilder.DropIndex(
                name: "IX_image_data_ReceivedAt",
                table: "image_data");

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
                name: "IX_telemetry_data_SatelliteId_GroundStationId_Timestamp",
                table: "telemetry_data",
                columns: new[] { "SatelliteId", "GroundStationId", "Timestamp" });

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

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_ApprovedById",
                table: "flight_plans",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_CreatedById",
                table: "flight_plans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_GroundStationId",
                table: "flight_plans",
                column: "GroundStationId");

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_OverpassId",
                table: "flight_plans",
                column: "OverpassId");

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_PreviousPlanId",
                table: "flight_plans",
                column: "PreviousPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_SatelliteId",
                table: "flight_plans",
                column: "SatelliteId");

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_flight_plans_PreviousPlanId",
                table: "flight_plans",
                column: "PreviousPlanId",
                principalTable: "flight_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_ground_stations_GroundStationId",
                table: "flight_plans",
                column: "GroundStationId",
                principalTable: "ground_stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_overpasses_OverpassId",
                table: "flight_plans",
                column: "OverpassId",
                principalTable: "overpasses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_satellites_SatelliteId",
                table: "flight_plans",
                column: "SatelliteId",
                principalTable: "satellites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_users_ApprovedById",
                table: "flight_plans",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_users_CreatedById",
                table: "flight_plans",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_image_data_ground_stations_GroundStationId",
                table: "image_data",
                column: "GroundStationId",
                principalTable: "ground_stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_image_data_ground_stations_GroundStationId1",
                table: "image_data",
                column: "GroundStationId1",
                principalTable: "ground_stations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_image_data_satellites_SatelliteId",
                table: "image_data",
                column: "SatelliteId",
                principalTable: "satellites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_telemetry_data_flight_plans_FlightPlanId",
                table: "telemetry_data",
                column: "FlightPlanId",
                principalTable: "flight_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_telemetry_data_ground_stations_GroundStationId",
                table: "telemetry_data",
                column: "GroundStationId",
                principalTable: "ground_stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_telemetry_data_ground_stations_GroundStationId1",
                table: "telemetry_data",
                column: "GroundStationId1",
                principalTable: "ground_stations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_telemetry_data_satellites_SatelliteId",
                table: "telemetry_data",
                column: "SatelliteId",
                principalTable: "satellites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_telemetry_data_satellites_SatelliteId1",
                table: "telemetry_data",
                column: "SatelliteId1",
                principalTable: "satellites",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_flight_plans_PreviousPlanId",
                table: "flight_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_ground_stations_GroundStationId",
                table: "flight_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_overpasses_OverpassId",
                table: "flight_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_satellites_SatelliteId",
                table: "flight_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_users_ApprovedById",
                table: "flight_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_users_CreatedById",
                table: "flight_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_image_data_ground_stations_GroundStationId",
                table: "image_data");

            migrationBuilder.DropForeignKey(
                name: "FK_image_data_ground_stations_GroundStationId1",
                table: "image_data");

            migrationBuilder.DropForeignKey(
                name: "FK_image_data_satellites_SatelliteId",
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
                name: "FK_telemetry_data_flight_plans_FlightPlanId",
                table: "telemetry_data");

            migrationBuilder.DropForeignKey(
                name: "FK_telemetry_data_ground_stations_GroundStationId",
                table: "telemetry_data");

            migrationBuilder.DropForeignKey(
                name: "FK_telemetry_data_ground_stations_GroundStationId1",
                table: "telemetry_data");

            migrationBuilder.DropForeignKey(
                name: "FK_telemetry_data_satellites_SatelliteId",
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
                name: "IX_telemetry_data_SatelliteId_GroundStationId_Timestamp",
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

            migrationBuilder.DropIndex(
                name: "IX_flight_plans_ApprovedById",
                table: "flight_plans");

            migrationBuilder.DropIndex(
                name: "IX_flight_plans_CreatedById",
                table: "flight_plans");

            migrationBuilder.DropIndex(
                name: "IX_flight_plans_GroundStationId",
                table: "flight_plans");

            migrationBuilder.DropIndex(
                name: "IX_flight_plans_OverpassId",
                table: "flight_plans");

            migrationBuilder.DropIndex(
                name: "IX_flight_plans_PreviousPlanId",
                table: "flight_plans");

            migrationBuilder.DropIndex(
                name: "IX_flight_plans_SatelliteId",
                table: "flight_plans");

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
                values: new object[] { new Guid("0f1bac5c-6250-442a-adb7-24c6a4cf73a2"), new DateTime(2025, 10, 17, 18, 50, 37, 617, DateTimeKind.Utc).AddTicks(8947), new DateTime(2025, 10, 17, 18, 50, 37, 617, DateTimeKind.Utc).AddTicks(8947) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 17, 18, 50, 37, 618, DateTimeKind.Utc).AddTicks(3694), new DateTime(2025, 10, 17, 18, 50, 37, 618, DateTimeKind.Utc).AddTicks(3694) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 17, 18, 50, 37, 618, DateTimeKind.Utc).AddTicks(3697), new DateTime(2025, 10, 17, 18, 50, 37, 618, DateTimeKind.Utc).AddTicks(3697) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });

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
                name: "IX_overpasses_EndTime",
                table: "overpasses",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_SatelliteId",
                table: "overpasses",
                column: "SatelliteId");

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_StartTime",
                table: "overpasses",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_ReceivedAt",
                table: "image_data",
                column: "ReceivedAt");
        }
    }
}
