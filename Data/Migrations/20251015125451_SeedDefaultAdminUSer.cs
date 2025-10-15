using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultAdminUSer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("fb8c6450-755c-42cf-bca2-cc0d8070a695"), new DateTime(2025, 10, 15, 12, 54, 51, 792, DateTimeKind.Utc).AddTicks(4025), new DateTime(2025, 10, 15, 12, 54, 51, 792, DateTimeKind.Utc).AddTicks(4025) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 15, 12, 54, 51, 792, DateTimeKind.Utc).AddTicks(7684), new DateTime(2025, 10, 15, 12, 54, 51, 792, DateTimeKind.Utc).AddTicks(7685) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 15, 12, 54, 51, 792, DateTimeKind.Utc).AddTicks(7719), new DateTime(2025, 10, 15, 12, 54, 51, 792, DateTimeKind.Utc).AddTicks(7719) });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "AdditionalRoles", "AdditionalScopes", "CreatedAt", "Email", "Name", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[] { 1, new List<string>(), new List<string>(), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@example.com", "Admin User", "$2a$11$N3CMfWFaZG7H.fuavEvLRuejsgLY25wYJXHMVFBxgxZvgiR4zha/.", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1);

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
    }
}
