using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuth0UserIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Auth0UserId",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
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

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes", "Auth0UserId" },
                values: new object[] { new List<string>(), new List<string>(), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Auth0UserId",
                table: "users");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("4e020e62-1bd8-4c92-881e-5745826db59d"), new DateTime(2025, 10, 23, 18, 22, 35, 216, DateTimeKind.Utc).AddTicks(2440), new DateTime(2025, 10, 23, 18, 22, 35, 216, DateTimeKind.Utc).AddTicks(2440) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 18, 22, 35, 216, DateTimeKind.Utc).AddTicks(2560), new DateTime(2025, 10, 23, 18, 22, 35, 216, DateTimeKind.Utc).AddTicks(2570) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 18, 22, 35, 216, DateTimeKind.Utc).AddTicks(2570), new DateTime(2025, 10, 23, 18, 22, 35, 216, DateTimeKind.Utc).AddTicks(2570) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });
        }
    }
}
