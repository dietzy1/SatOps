using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class AddOverpassIdToFlightPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OverpassId",
                table: "flight_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 1, 9, 50, 35, 569, DateTimeKind.Utc).AddTicks(890), new DateTime(2025, 10, 1, 9, 50, 35, 569, DateTimeKind.Utc).AddTicks(890) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 9, 50, 35, 569, DateTimeKind.Utc).AddTicks(6120), new DateTime(2025, 10, 1, 9, 50, 35, 569, DateTimeKind.Utc).AddTicks(6120) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 9, 50, 35, 569, DateTimeKind.Utc).AddTicks(6120), new DateTime(2025, 10, 1, 9, 50, 35, 569, DateTimeKind.Utc).AddTicks(6120) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverpassId",
                table: "flight_plans");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 15, 21, 2, 972, DateTimeKind.Utc).AddTicks(6390), new DateTime(2025, 9, 29, 15, 21, 2, 972, DateTimeKind.Utc).AddTicks(6390) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 9, 29, 15, 21, 2, 973, DateTimeKind.Utc).AddTicks(1880), new DateTime(2025, 9, 29, 15, 21, 2, 973, DateTimeKind.Utc).AddTicks(1880) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 9, 29, 15, 21, 2, 973, DateTimeKind.Utc).AddTicks(1880), new DateTime(2025, 9, 29, 15, 21, 2, 973, DateTimeKind.Utc).AddTicks(1880) });
        }
    }
}
