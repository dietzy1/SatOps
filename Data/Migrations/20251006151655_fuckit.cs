using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class fuckit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Body",
                table: "flight_plans",
                newName: "Commands");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("4a5193c8-5d08-4816-9e66-86db81e6d326"), new DateTime(2025, 10, 6, 15, 16, 55, 53, DateTimeKind.Utc).AddTicks(2580), new DateTime(2025, 10, 6, 15, 16, 55, 53, DateTimeKind.Utc).AddTicks(2580) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 6, 15, 16, 55, 53, DateTimeKind.Utc).AddTicks(6900), new DateTime(2025, 10, 6, 15, 16, 55, 53, DateTimeKind.Utc).AddTicks(6900) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 6, 15, 16, 55, 53, DateTimeKind.Utc).AddTicks(6900), new DateTime(2025, 10, 6, 15, 16, 55, 53, DateTimeKind.Utc).AddTicks(6900) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Commands",
                table: "flight_plans",
                newName: "Body");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("f6c69f82-6fb7-4f32-a2eb-fe32afaef99a"), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(4560), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(4560) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850) });
        }
    }
}
