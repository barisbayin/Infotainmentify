using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TopicGenerationProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicGenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    PromptId = table.Column<int>(type: "int", nullable: false),
                    AiConnectionId = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedCount = table.Column<int>(type: "int", nullable: false),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicGenerationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicGenerationProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicGenerationProfiles_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicGenerationProfiles_UserAiConnections_AiConnectionId",
                        column: x => x.AiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_AiConnectionId",
                table: "TopicGenerationProfiles",
                column: "AiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_AppUserId",
                table: "TopicGenerationProfiles",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_CreatedAt",
                table: "TopicGenerationProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_PromptId",
                table: "TopicGenerationProfiles",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_Removed",
                table: "TopicGenerationProfiles",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_StartedAt",
                table: "TopicGenerationProfiles",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_Status",
                table: "TopicGenerationProfiles",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicGenerationProfiles");
        }
    }
}
