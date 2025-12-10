using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.Data.migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedImageDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageHeight",
                table: "image_data");

            migrationBuilder.DropColumn(
                name: "ImageWidth",
                table: "image_data");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "image_data");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("250749f0-4728-4a87-8d94-a83df2bffe77"), new DateTime(2025, 12, 9, 20, 37, 9, 846, DateTimeKind.Utc).AddTicks(6090), new DateTime(2025, 12, 9, 20, 37, 9, 846, DateTimeKind.Utc).AddTicks(6090) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 12, 9, 20, 37, 9, 846, DateTimeKind.Utc).AddTicks(6260), new DateTime(2025, 12, 9, 20, 37, 9, 846, DateTimeKind.Utc).AddTicks(6260) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 12, 9, 20, 37, 9, 846, DateTimeKind.Utc).AddTicks(6260), new DateTime(2025, 12, 9, 20, 37, 9, 846, DateTimeKind.Utc).AddTicks(6260) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageHeight",
                table: "image_data",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageWidth",
                table: "image_data",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "image_data",
                type: "jsonb",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("c4bd6c61-8e11-43cf-868a-fdd0a4be81b2"), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9380), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9380) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710), new DateTime(2025, 12, 9, 14, 19, 32, 968, DateTimeKind.Utc).AddTicks(9710) });
        }
    }
}
