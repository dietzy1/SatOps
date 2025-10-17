using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOverpassForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddForeignKey(
                name: "FK_overpasses_ground_stations_GroundStationId",
                table: "overpasses",
                column: "GroundStationId",
                principalTable: "ground_stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_overpasses_satellites_SatelliteId",
                table: "overpasses",
                column: "SatelliteId",
                principalTable: "satellites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_overpasses_ground_stations_GroundStationId",
                table: "overpasses");

            migrationBuilder.DropForeignKey(
                name: "FK_overpasses_satellites_SatelliteId",
                table: "overpasses");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("7f143027-9c61-4f14-a7be-4524fb5b61f6"), new DateTime(2025, 10, 16, 11, 49, 4, 386, DateTimeKind.Utc).AddTicks(1849), new DateTime(2025, 10, 16, 11, 49, 4, 386, DateTimeKind.Utc).AddTicks(1849) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 16, 11, 49, 4, 386, DateTimeKind.Utc).AddTicks(6905), new DateTime(2025, 10, 16, 11, 49, 4, 386, DateTimeKind.Utc).AddTicks(6905) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 16, 11, 49, 4, 386, DateTimeKind.Utc).AddTicks(6908), new DateTime(2025, 10, 16, 11, 49, 4, 386, DateTimeKind.Utc).AddTicks(6909) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });
        }
    }
}
