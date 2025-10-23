using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameHttpUrlToWebSocketUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HttpUrl",
                table: "ground_stations",
                newName: "websocket_url");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "websocket_url",
                table: "ground_stations",
                newName: "HttpUrl");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "HttpUrl", "UpdatedAt" },
                values: new object[] { new Guid("86379ca2-eca8-443b-9620-33994337eb08"), new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3090), "http://aarhus-groundstation.example.com", new DateTime(2025, 10, 23, 17, 30, 18, 732, DateTimeKind.Utc).AddTicks(3090) });

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
    }
}
