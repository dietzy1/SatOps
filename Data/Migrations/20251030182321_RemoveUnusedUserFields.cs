using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "AdditionalRoles",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AdditionalScopes",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "users");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("9d77315c-6b7c-4048-bbe3-3bfaa211a8d9"), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(6810), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(6810) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7020), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7020) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7030), new DateTime(2025, 10, 30, 18, 23, 20, 878, DateTimeKind.Utc).AddTicks(7030) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "AdditionalRoles",
                table: "users",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "AdditionalScopes",
                table: "users",
                type: "jsonb",
                nullable: false);

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
                values: new object[] { new Guid("992c768c-4fec-427c-9aea-d7d4c08a34c2"), new DateTime(2025, 10, 30, 10, 56, 15, 707, DateTimeKind.Utc).AddTicks(1170), new DateTime(2025, 10, 30, 10, 56, 15, 707, DateTimeKind.Utc).AddTicks(1170) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 30, 10, 56, 15, 707, DateTimeKind.Utc).AddTicks(1290), new DateTime(2025, 10, 30, 10, 56, 15, 707, DateTimeKind.Utc).AddTicks(1290) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 30, 10, 56, 15, 707, DateTimeKind.Utc).AddTicks(1290), new DateTime(2025, 10, 30, 10, 56, 15, 707, DateTimeKind.Utc).AddTicks(1290) });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "AdditionalRoles", "AdditionalScopes", "Auth0UserId", "CreatedAt", "Email", "Name", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[] { 1, new List<string>(), new List<string>(), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@example.com", "Admin User", "$2a$11$N3CMfWFaZG7H.fuavEvLRuejsgLY25wYJXHMVFBxgxZvgiR4zha/.", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
