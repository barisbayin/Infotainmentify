using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductionProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductionProfile",
                table: "ContentPipelineTemplates",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Generic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductionProfile",
                table: "ContentPipelineTemplates");
        }
    }
}
