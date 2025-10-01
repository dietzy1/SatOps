using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyToGroundStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHash",
                table: "ground_stations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "ground_stations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApiKeyHash", "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { "", new Guid("9b563e97-6c68-4e2c-86c0-565c572479ee"), new DateTime(2025, 10, 1, 10, 39, 13, 836, DateTimeKind.Utc).AddTicks(9664), new DateTime(2025, 10, 1, 10, 39, 13, 836, DateTimeKind.Utc).AddTicks(9665) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 10, 39, 13, 837, DateTimeKind.Utc).AddTicks(3907), new DateTime(2025, 10, 1, 10, 39, 13, 837, DateTimeKind.Utc).AddTicks(3908) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 10, 39, 13, 837, DateTimeKind.Utc).AddTicks(3911), new DateTime(2025, 10, 1, 10, 39, 13, 837, DateTimeKind.Utc).AddTicks(3911) });

            migrationBuilder.CreateIndex(
                name: "IX_ground_stations_ApplicationId",
                table: "ground_stations",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ground_stations_ApplicationId",
                table: "ground_stations");

            migrationBuilder.DropColumn(
                name: "ApiKeyHash",
                table: "ground_stations");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "ground_stations");

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
