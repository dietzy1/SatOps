using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixFlightPlanOverpassRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_flight_plans_overpasses_OverpassId",
                table: "flight_plans");

            migrationBuilder.DropIndex(
                name: "IX_overpasses_FlightPlanId",
                table: "overpasses");

            migrationBuilder.DropIndex(
                name: "IX_flight_plans_OverpassId",
                table: "flight_plans");

            migrationBuilder.DropColumn(
                name: "OverpassId",
                table: "flight_plans");

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

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_FlightPlanId",
                table: "overpasses",
                column: "FlightPlanId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_overpasses_FlightPlanId",
                table: "overpasses");

            migrationBuilder.AddColumn<int>(
                name: "OverpassId",
                table: "flight_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("5a69c05e-4612-4ea9-940b-13398b689750"), new DateTime(2025, 11, 11, 17, 31, 43, 47, DateTimeKind.Utc).AddTicks(7580), new DateTime(2025, 11, 11, 17, 31, 43, 47, DateTimeKind.Utc).AddTicks(7580) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 11, 17, 31, 43, 47, DateTimeKind.Utc).AddTicks(7810), new DateTime(2025, 11, 11, 17, 31, 43, 47, DateTimeKind.Utc).AddTicks(7820) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 11, 17, 31, 43, 47, DateTimeKind.Utc).AddTicks(7820), new DateTime(2025, 11, 11, 17, 31, 43, 47, DateTimeKind.Utc).AddTicks(7820) });

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_FlightPlanId",
                table: "overpasses",
                column: "FlightPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_OverpassId",
                table: "flight_plans",
                column: "OverpassId");

            migrationBuilder.AddForeignKey(
                name: "FK_flight_plans_overpasses_OverpassId",
                table: "flight_plans",
                column: "OverpassId",
                principalTable: "overpasses",
                principalColumn: "Id");
        }
    }
}
