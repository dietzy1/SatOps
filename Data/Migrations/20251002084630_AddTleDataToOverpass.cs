using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class AddTleDataToOverpass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TleLine1",
                table: "overpasses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TleLine2",
                table: "overpasses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TleUpdateTime",
                table: "overpasses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("587ccace-a001-4f6e-b89f-bfc1c0a8dc9f"), new DateTime(2025, 10, 2, 8, 46, 29, 902, DateTimeKind.Utc).AddTicks(5590), new DateTime(2025, 10, 2, 8, 46, 29, 902, DateTimeKind.Utc).AddTicks(5590) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 2, 8, 46, 29, 903, DateTimeKind.Utc).AddTicks(870), new DateTime(2025, 10, 2, 8, 46, 29, 903, DateTimeKind.Utc).AddTicks(870) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 2, 8, 46, 29, 903, DateTimeKind.Utc).AddTicks(880), new DateTime(2025, 10, 2, 8, 46, 29, 903, DateTimeKind.Utc).AddTicks(880) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TleLine1",
                table: "overpasses");

            migrationBuilder.DropColumn(
                name: "TleLine2",
                table: "overpasses");

            migrationBuilder.DropColumn(
                name: "TleUpdateTime",
                table: "overpasses");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("ea3da4e4-d012-4c34-9c96-3add56639761"), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(330), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(330) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170) });
        }
    }
}
