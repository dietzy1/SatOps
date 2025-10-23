using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropWebSocketUrlColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("34bfac3d-4a58-4574-b891-851074104f8e"), new DateTime(2025, 10, 23, 18, 22, 10, 803, DateTimeKind.Utc).AddTicks(290), new DateTime(2025, 10, 23, 18, 22, 10, 803, DateTimeKind.Utc).AddTicks(290) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 18, 22, 10, 803, DateTimeKind.Utc).AddTicks(430), new DateTime(2025, 10, 23, 18, 22, 10, 803, DateTimeKind.Utc).AddTicks(430) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 18, 22, 10, 803, DateTimeKind.Utc).AddTicks(430), new DateTime(2025, 10, 23, 18, 22, 10, 803, DateTimeKind.Utc).AddTicks(430) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });
        }
    }
}
