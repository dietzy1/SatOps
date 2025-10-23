using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsActiveFromGroundStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ground_stations");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("86379ca2-eca8-443b-9620-33994337eb08"), new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3090), new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3090) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3230), new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3230) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3240), new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3240) });

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
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ground_stations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("c5cdbad5-95da-4cca-8f7d-c137fe79fe5a"), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(7971), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(7971) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8086), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8087) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8088), new DateTime(2025, 10, 19, 20, 8, 33, 334, DateTimeKind.Utc).AddTicks(8089) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });
        }
    }
}
