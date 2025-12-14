using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedAssetFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhysicalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSec = table.Column<double>(type: "float", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "application/octet-stream"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetFiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetFiles_AppUserId_Type",
                table: "AssetFiles",
                columns: new[] { "AppUserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetFiles_CreatedAt",
                table: "AssetFiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssetFiles_Removed",
                table: "AssetFiles",
                column: "Removed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetFiles");
        }
    }
}
