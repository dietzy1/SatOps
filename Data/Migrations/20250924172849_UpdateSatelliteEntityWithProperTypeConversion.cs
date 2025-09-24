using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SatOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSatelliteEntityWithProperTypeConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTleUpdate",
                table: "satellites");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ground_stations");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "satellites",
                newName: "LastUpdate");

            migrationBuilder.AlterColumn<string>(
                name: "TleLine2",
                table: "satellites",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TleLine1",
                table: "satellites",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Custom SQL to convert NoradId from text to integer using PostgreSQL USING clause
            migrationBuilder.Sql("ALTER TABLE satellites ALTER COLUMN \"NoradId\" TYPE integer USING \"NoradId\"::integer;");

            migrationBuilder.AddColumn<double>(
                name: "altitude",
                table: "ground_stations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "ground_stations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "ground_stations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    AdditionalScopes = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    AdditionalRoles = table.Column<string>(type: "text", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt", "latitude", "longitude" },
                values: new object[] { new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(4150), new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(4150), 56.1629, 10.203900000000001 });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate", "NoradId" },
                values: new object[] { new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(5860), new DateTime(2025, 9, 24, 17, 28, 49, 659, DateTimeKind.Utc).AddTicks(5860), 25544 });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Role",
                table: "users",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropColumn(
                name: "altitude",
                table: "ground_stations");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "ground_stations");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "ground_stations");

            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "satellites",
                newName: "UpdatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "TleLine2",
                table: "satellites",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TleLine1",
                table: "satellites",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // Custom SQL to convert NoradId from integer back to text
            migrationBuilder.Sql("ALTER TABLE satellites ALTER COLUMN \"NoradId\" TYPE text USING \"NoradId\"::text;");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTleUpdate",
                table: "satellites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "ground_stations",
                type: "geometry(Point,4326)",
                nullable: false);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Location", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9730), (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (10.2039 56.1629)"), new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9740) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastTleUpdate", "NoradId", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9800), new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9800), "25544", new DateTime(2025, 9, 13, 15, 59, 36, 990, DateTimeKind.Utc).AddTicks(9800) });
        }
    }
}
