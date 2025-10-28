using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeletedColums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessTokenExpiresAt",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "AuthType",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "Capabilities",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "IsDefaultForEmbedding",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "IsDefaultForImage",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "IsDefaultForText",
                table: "UserAiConnections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccessTokenExpiresAt",
                table: "UserAiConnections",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthType",
                table: "UserAiConnections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Capabilities",
                table: "UserAiConnections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForEmbedding",
                table: "UserAiConnections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForImage",
                table: "UserAiConnections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForText",
                table: "UserAiConnections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
