using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("c6ca6523-abaa-4128-b695-fcaea8a6a600"), new DateTime(2025, 10, 15, 11, 8, 50, 135, DateTimeKind.Utc).AddTicks(2131), new DateTime(2025, 10, 15, 11, 8, 50, 135, DateTimeKind.Utc).AddTicks(2133) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 15, 11, 8, 50, 135, DateTimeKind.Utc).AddTicks(5781), new DateTime(2025, 10, 15, 11, 8, 50, 135, DateTimeKind.Utc).AddTicks(5782) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 15, 11, 8, 50, 135, DateTimeKind.Utc).AddTicks(5785), new DateTime(2025, 10, 15, 11, 8, 50, 135, DateTimeKind.Utc).AddTicks(5785) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "users");

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
    }
}
