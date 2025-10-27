using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PremiseTr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Premise = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PotentialVisual = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedsFootage = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FactCheck = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopicJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromptId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Topics_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Category",
                table: "Prompts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_CreatedAt",
                table: "Prompts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_IsActive",
                table: "Prompts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Name",
                table: "Prompts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Removed",
                table: "Prompts",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Category",
                table: "Topics",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_PromptId",
                table: "Topics",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_TopicCode",
                table: "Topics",
                column: "TopicCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "Prompts");
        }
    }
}
