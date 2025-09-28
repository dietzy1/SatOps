using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveToOperationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "command_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    CommandType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommandPayload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "image_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SatelliteId = table.Column<int>(type: "integer", nullable: false),
                    GroundStationId = table.Column<int>(type: "integer", nullable: false),
                    CaptureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    S3ObjectPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    Latitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: true),
                    ImageWidth = table.Column<int>(type: "integer", nullable: true),
                    ImageHeight = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image_data", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 25, 12, 6, 51, 711, DateTimeKind.Utc).AddTicks(2060), new DateTime(2025, 9, 25, 12, 6, 51, 711, DateTimeKind.Utc).AddTicks(2060) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 9, 25, 12, 6, 51, 711, DateTimeKind.Utc).AddTicks(7100), new DateTime(2025, 9, 25, 12, 6, 51, 711, DateTimeKind.Utc).AddTicks(7100) });

            migrationBuilder.CreateIndex(
                name: "IX_command_data_CreatedAt",
                table: "command_data",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_command_data_ExpiresAt",
                table: "command_data",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_command_data_GroundStationId",
                table: "command_data",
                column: "GroundStationId");

            migrationBuilder.CreateIndex(
                name: "IX_command_data_SatelliteId",
                table: "command_data",
                column: "SatelliteId");

            migrationBuilder.CreateIndex(
                name: "IX_command_data_Status",
                table: "command_data",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_CaptureTime",
                table: "image_data",
                column: "CaptureTime");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_GroundStationId",
                table: "image_data",
                column: "GroundStationId");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_Latitude_Longitude",
                table: "image_data",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_image_data_ReceivedAt",
                table: "image_data",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_image_data_SatelliteId",
                table: "image_data",
                column: "SatelliteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_data");

            migrationBuilder.DropTable(
                name: "image_data");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 24, 20, 7, 29, 834, DateTimeKind.Utc).AddTicks(8000), new DateTime(2025, 9, 24, 20, 7, 29, 834, DateTimeKind.Utc).AddTicks(8000) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 9, 24, 20, 7, 29, 835, DateTimeKind.Utc).AddTicks(480), new DateTime(2025, 9, 24, 20, 7, 29, 835, DateTimeKind.Utc).AddTicks(480) });
        }
    }
}
