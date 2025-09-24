using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "flight_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GroundStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SatelliteName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PreviousPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApproverId = table.Column<string>(type: "text", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flight_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ground_stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point,4326)", nullable: false),
                    HttpUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ground_stations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ground_stations",
                columns: new[] { "Id", "CreatedAt", "HttpUrl", "Location", "Name" },
                values: new object[] { 1, new DateTime(2025, 9, 11, 18, 46, 24, 406, DateTimeKind.Utc).AddTicks(7320), "http://aarhus-groundstation.example.com", (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (10.2039 56.1629)"), "Aarhus" });

            migrationBuilder.CreateIndex(
                name: "IX_flight_plans_Status",
                table: "flight_plans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flight_plans");

            migrationBuilder.DropTable(
                name: "ground_stations");
        }
    }
}
