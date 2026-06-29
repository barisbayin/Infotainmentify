using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SavedProductionBriefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedProductionBriefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ConceptId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    MainTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Angle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetDuration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MustCover = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    Avoid = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedProductionBriefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedProductionBriefs_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedProductionBriefs_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedProductionBriefs_AppUserId_ConceptId",
                table: "SavedProductionBriefs",
                columns: new[] { "AppUserId", "ConceptId" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedProductionBriefs_AppUserId_Name",
                table: "SavedProductionBriefs",
                columns: new[] { "AppUserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedProductionBriefs_ConceptId",
                table: "SavedProductionBriefs",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedProductionBriefs_CreatedAt",
                table: "SavedProductionBriefs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedProductionBriefs_LastUsedAt",
                table: "SavedProductionBriefs",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedProductionBriefs_Removed",
                table: "SavedProductionBriefs",
                column: "Removed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedProductionBriefs");
        }
    }
}
