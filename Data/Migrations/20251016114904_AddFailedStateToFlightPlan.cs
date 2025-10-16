using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFailedStateToFlightPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "flight_plans",
                type: "text",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "flight_plans");

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

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });
        }
    }
}
