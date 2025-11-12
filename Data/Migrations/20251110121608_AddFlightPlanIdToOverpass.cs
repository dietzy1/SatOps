using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightPlanIdToOverpass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlightPlanId",
                table: "overpasses",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("dfef8181-269c-4932-b41e-41fb2c306577"), new DateTime(2025, 11, 10, 12, 16, 7, 952, DateTimeKind.Utc).AddTicks(6740), new DateTime(2025, 11, 10, 12, 16, 7, 952, DateTimeKind.Utc).AddTicks(6740) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 10, 12, 16, 7, 952, DateTimeKind.Utc).AddTicks(6930), new DateTime(2025, 11, 10, 12, 16, 7, 952, DateTimeKind.Utc).AddTicks(6930) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 10, 12, 16, 7, 952, DateTimeKind.Utc).AddTicks(6930), new DateTime(2025, 11, 10, 12, 16, 7, 952, DateTimeKind.Utc).AddTicks(6930) });

            migrationBuilder.CreateIndex(
                name: "IX_overpasses_FlightPlanId",
                table: "overpasses",
                column: "FlightPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_overpasses_flight_plans_FlightPlanId",
                table: "overpasses",
                column: "FlightPlanId",
                principalTable: "flight_plans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_overpasses_flight_plans_FlightPlanId",
                table: "overpasses");

            migrationBuilder.DropIndex(
                name: "IX_overpasses_FlightPlanId",
                table: "overpasses");

            migrationBuilder.DropColumn(
                name: "FlightPlanId",
                table: "overpasses");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("33c7f00a-d3d4-4dd3-98d9-2f2b73dc255e"), new DateTime(2025, 11, 10, 7, 40, 48, 321, DateTimeKind.Utc).AddTicks(8730), new DateTime(2025, 11, 10, 7, 40, 48, 321, DateTimeKind.Utc).AddTicks(8740) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 10, 7, 40, 48, 321, DateTimeKind.Utc).AddTicks(8910), new DateTime(2025, 11, 10, 7, 40, 48, 321, DateTimeKind.Utc).AddTicks(8910) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 10, 7, 40, 48, 321, DateTimeKind.Utc).AddTicks(8910), new DateTime(2025, 11, 10, 7, 40, 48, 321, DateTimeKind.Utc).AddTicks(8910) });
        }
    }
}
