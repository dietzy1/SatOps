using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWebSocketUrlFromGroundStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "websocket_url",
                table: "ground_stations");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "websocket_url",
                table: "ground_stations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt", "websocket_url" },
                values: new object[] { new Guid("a34213de-796c-46ab-bada-55efb0627b2e"), new DateTime(2025, 10, 23, 17, 49, 24, 776, DateTimeKind.Utc).AddTicks(250), new DateTime(2025, 10, 23, 17, 49, 24, 776, DateTimeKind.Utc).AddTicks(250), "ws://aarhus-groundstation.example.com" });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 17, 49, 24, 776, DateTimeKind.Utc).AddTicks(390), new DateTime(2025, 10, 23, 17, 49, 24, 776, DateTimeKind.Utc).AddTicks(400) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 23, 17, 49, 24, 776, DateTimeKind.Utc).AddTicks(400), new DateTime(2025, 10, 23, 17, 49, 24, 776, DateTimeKind.Utc).AddTicks(400) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdditionalRoles", "AdditionalScopes" },
                values: new object[] { new List<string>(), new List<string>() });
        }
    }
}
