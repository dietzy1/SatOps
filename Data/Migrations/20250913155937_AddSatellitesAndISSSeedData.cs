using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSatellitesAndISSSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ground_stations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "satellites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    NoradId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TleLine1 = table.Column<string>(type: "text", nullable: true),
                    TleLine2 = table.Column<string>(type: "text", nullable: true),
                    LastTleUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_satellites", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9730), new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9740) });

            migrationBuilder.InsertData(
                table: "satellites",
                columns: new[] { "Id", "CreatedAt", "LastTleUpdate", "Name", "NoradId", "Status", "TleLine1", "TleLine2", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9800), new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9800), "International Space Station (ISS)", "25544", 0, "1 25544U 98067A   23256.90616898  .00020137  00000-0  35438-3 0  9992", "2 25544  51.6416 339.0970 0003835  48.3825  73.2709 15.50030022414673", new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9800) });

            migrationBuilder.CreateIndex(
                name: "IX_satellites_NoradId",
                table: "satellites",
                column: "NoradId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_satellites_Status",
                table: "satellites",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "satellites");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ground_stations");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 11, 18, 46, 24, 406, DateTimeKind.Utc).AddTicks(7320));
        }
    }
}
