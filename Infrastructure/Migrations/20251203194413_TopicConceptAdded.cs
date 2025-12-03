using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TopicConceptAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConceptId",
                table: "Topics",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_ConceptId",
                table: "Topics",
                column: "ConceptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_Concepts_ConceptId",
                table: "Topics",
                column: "ConceptId",
                principalTable: "Concepts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Topics_Concepts_ConceptId",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Topics_ConceptId",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "ConceptId",
                table: "Topics");
        }
    }
}
