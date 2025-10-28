using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserAiConnectionAddColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageModel",
                table: "UserAiConnections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "UserAiConnections",
                type: "decimal(3,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextModel",
                table: "UserAiConnections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageModel",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "TextModel",
                table: "UserAiConnections");
        }
    }
}
