using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatOps.data.migrations
{
    /// <inheritdoc />
    public partial class UpdateUserPermissionsToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE users ALTER COLUMN ""AdditionalScopes"" DROP DEFAULT;

                ALTER TABLE users ALTER COLUMN ""AdditionalScopes"" TYPE jsonb 
                USING (
                    CASE 
                        WHEN ""AdditionalScopes"" IS NULL OR ""AdditionalScopes"" = '' THEN '[]'::jsonb
                        ELSE to_jsonb(string_to_array(""AdditionalScopes"", ',')) 
                    END
                );

                ALTER TABLE users ALTER COLUMN ""AdditionalScopes"" SET DEFAULT '[]'::jsonb;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE users ALTER COLUMN ""AdditionalRoles"" DROP DEFAULT;

                ALTER TABLE users ALTER COLUMN ""AdditionalRoles"" TYPE jsonb 
                USING (
                    CASE 
                        WHEN ""AdditionalRoles"" IS NULL OR ""AdditionalRoles"" = '' THEN '[]'::jsonb
                        ELSE to_jsonb(string_to_array(""AdditionalRoles"", ',')) 
                    END
                );
                
                ALTER TABLE users ALTER COLUMN ""AdditionalRoles"" SET DEFAULT '[]'::jsonb;
            ");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("f1e7e305-51da-4a7a-898a-aa57939ab0e6"), new DateTime(2025, 10, 2, 10, 21, 38, 954, DateTimeKind.Utc).AddTicks(7023), new DateTime(2025, 10, 2, 10, 21, 38, 954, DateTimeKind.Utc).AddTicks(7025) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 2, 10, 21, 38, 955, DateTimeKind.Utc).AddTicks(723), new DateTime(2025, 10, 2, 10, 21, 38, 955, DateTimeKind.Utc).AddTicks(724) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 2, 10, 21, 38, 955, DateTimeKind.Utc).AddTicks(726), new DateTime(2025, 10, 2, 10, 21, 38, 955, DateTimeKind.Utc).AddTicks(727) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE users ALTER COLUMN ""AdditionalScopes"" DROP DEFAULT;
                
                ALTER TABLE users ALTER COLUMN ""AdditionalScopes"" TYPE text 
                USING (array_to_string(ARRAY(SELECT jsonb_array_elements_text(""AdditionalScopes"")), ','));
                
                ALTER TABLE users ALTER COLUMN ""AdditionalScopes"" SET DEFAULT '';
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE users ALTER COLUMN ""AdditionalRoles"" DROP DEFAULT;

                ALTER TABLE users ALTER COLUMN ""AdditionalRoles"" TYPE text 
                USING (array_to_string(ARRAY(SELECT jsonb_array_elements_text(""AdditionalRoles"")), ','));

                ALTER TABLE users ALTER COLUMN ""AdditionalRoles"" SET DEFAULT '';
            ");

            migrationBuilder.UpdateData(
                table: "ground_stations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApplicationId", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("ea3da4e4-d012-4c34-9c96-3add56639761"), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(330), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(330) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170) });

            migrationBuilder.UpdateData(
                table: "satellites",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastUpdate" },
                values: new object[] { new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170), new DateTime(2025, 10, 1, 14, 41, 12, 246, DateTimeKind.Utc).AddTicks(9170) });
        }
    }
}
