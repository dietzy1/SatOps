using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class flightenum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "flight_plans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ScheduledAt",
                table: "flight_plans",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 1, 14, 33, 20, 365, DateTimeKind.Utc).AddTicks(5160), new DateTime(2025, 10, 1, 14, 33, 20, 365, DateTimeKind.Utc).AddTicks(5160) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 14, 33, 20, 366, DateTimeKind.Utc).AddTicks(1010), new DateTime(2025, 10, 1, 14, 33, 20, 366, DateTimeKind.Utc).AddTicks(1010) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 14, 33, 20, 366, DateTimeKind.Utc).AddTicks(1010), new DateTime(2025, 10, 1, 14, 33, 20, 366, DateTimeKind.Utc).AddTicks(1010) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "flight_plans",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ScheduledAt",
                table: "flight_plans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

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
    }
}
