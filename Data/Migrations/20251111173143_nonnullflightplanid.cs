using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class nonnullflightplanid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_overpasses_flight_plans_FlightPlanId",
                table: "overpasses");

            migrationBuilder.AlterColumn<int>(
                name: "FlightPlanId",
                table: "overpasses",
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

            migrationBuilder.AddForeignKey(
                name: "FK_overpasses_flight_plans_FlightPlanId",
                table: "overpasses",
                column: "FlightPlanId",
                principalTable: "flight_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_overpasses_flight_plans_FlightPlanId",
                table: "overpasses");

            migrationBuilder.AlterColumn<int>(
                name: "FlightPlanId",
                table: "overpasses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

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

            migrationBuilder.AddForeignKey(
                name: "FK_overpasses_flight_plans_FlightPlanId",
                table: "overpasses",
                column: "FlightPlanId",
                principalTable: "flight_plans",
                principalColumn: "Id");
        }
    }
}
