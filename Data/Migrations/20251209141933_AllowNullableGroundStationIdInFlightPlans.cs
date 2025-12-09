using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.migrations
{
    /// <inheritdoc />
    public partial class AllowNullableGroundStationIdInFlightPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_ground_stations_GroundStationId",
                table: "flight_plans");

            migrationBuilder.AlterColumn<int>(
                name: "GroundStationId",
                table: "flight_plans",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("c4bd6c61-8e11-43cf-868a-fdd0a4be81b2"), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9380), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9380) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710) });

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_ground_stations_GroundStationId",
                table: "flight_plans",
                column: "GroundStationId",
                principalTable: "ground_stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_ground_stations_GroundStationId",
                table: "flight_plans");

            migrationBuilder.AlterColumn<int>(
                name: "GroundStationId",
                table: "flight_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("eecf3fa7-de5b-41e0-83ac-ce6d14cd9b86"), new DateTime(2025, 11, 12, 20, 16, 41, 276, DateTimeKind.Utc).AddTicks(5207), new DateTime(2025, 11, 12, 20, 16, 41, 276, DateTimeKind.Utc).AddTicks(5208) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 12, 20, 16, 41, 276, DateTimeKind.Utc).AddTicks(5387), new DateTime(2025, 11, 12, 20, 16, 41, 276, DateTimeKind.Utc).AddTicks(5388) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 12, 20, 16, 41, 276, DateTimeKind.Utc).AddTicks(5391), new DateTime(2025, 11, 12, 20, 16, 41, 276, DateTimeKind.Utc).AddTicks(5391) });

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_ground_stations_GroundStationId",
                table: "flight_plans",
                column: "GroundStationId",
                principalTable: "ground_stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
