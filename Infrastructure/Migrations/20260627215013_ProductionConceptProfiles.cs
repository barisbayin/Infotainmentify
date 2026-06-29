using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductionConceptProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputConceptProfileJson",
                table: "ContentPipelineRuns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionConceptProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ConceptId = table.Column<int>(type: "int", nullable: false),
                    ProductionProfile = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "LongForm"),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "en-US"),
                    DefaultPlatform = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, defaultValue: "YouTube"),
                    Audience = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Tone = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    ChannelPromise = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    VisualStyleName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    VisualStyleBible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CharacterBible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TextPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultDurationSec = table.Column<int>(type: "int", nullable: true),
                    DefaultTemplateId = table.Column<int>(type: "int", nullable: true),
                    DefaultReviewPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionConceptProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionConceptProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionConceptProfiles_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionConceptProfiles_ContentPipelineTemplates_DefaultTemplateId",
                        column: x => x.DefaultTemplateId,
                        principalTable: "ContentPipelineTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConceptProfiles_AppUserId_ConceptId",
                table: "ProductionConceptProfiles",
                columns: new[] { "AppUserId", "ConceptId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConceptProfiles_ConceptId",
                table: "ProductionConceptProfiles",
                column: "ConceptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConceptProfiles_CreatedAt",
                table: "ProductionConceptProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConceptProfiles_DefaultTemplateId",
                table: "ProductionConceptProfiles",
                column: "DefaultTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConceptProfiles_ProductionProfile",
                table: "ProductionConceptProfiles",
                column: "ProductionProfile");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConceptProfiles_Removed",
                table: "ProductionConceptProfiles",
                column: "Removed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionConceptProfiles");

            migrationBuilder.DropColumn(
                name: "InputConceptProfileJson",
                table: "ContentPipelineRuns");
        }
    }
}
