using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightPlanIdToImageData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlightPlanId",
                table: "image_data",
                type: "integer",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_image_data_FlightPlanId",
                table: "image_data",
                column: "FlightPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_image_data_flight_plans_FlightPlanId",
                table: "image_data",
                column: "FlightPlanId",
                principalTable: "flight_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_image_data_flight_plans_FlightPlanId",
                table: "image_data");

            migrationBuilder.DropIndex(
                name: "IX_image_data_FlightPlanId",
                table: "image_data");

            migrationBuilder.DropColumn(
                name: "FlightPlanId",
                table: "image_data");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("a76767d0-a48e-44ed-ac86-19ccefb0ac71"), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3300), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3300) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450), new DateTime(2025, 11, 9, 19, 34, 3, 888, DateTimeKind.Utc).AddTicks(3450) });
        }
    }
}
