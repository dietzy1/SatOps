using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class UpdateFlightPlanFieldsAndCommandStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApproverId",
                table: "flight_plans");

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "flight_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "flight_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("f6c69f82-6fb7-4f32-a2eb-fe32afaef99a"), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(4560), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(4560) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850), new DateTime(2025, 10, 6, 6, 44, 42, 71, DateTimeKind.Utc).AddTicks(9850) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "flight_plans");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "flight_plans");

            migrationBuilder.AddColumn<string>(
                name: "ApproverId",
                table: "flight_plans",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("b599aa3f-05d2-4076-8b00-7a542be063fc"), new DateTime(2025, 10, 2, 11, 39, 29, 756, DateTimeKind.Utc).AddTicks(7200), new DateTime(2025, 10, 2, 11, 39, 29, 756, DateTimeKind.Utc).AddTicks(7200) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 2, 11, 39, 29, 757, DateTimeKind.Utc).AddTicks(2080), new DateTime(2025, 10, 2, 11, 39, 29, 757, DateTimeKind.Utc).AddTicks(2080) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 2, 11, 39, 29, 757, DateTimeKind.Utc).AddTicks(2080), new DateTime(2025, 10, 2, 11, 39, 29, 757, DateTimeKind.Utc).AddTicks(2080) });
        }
    }
}
