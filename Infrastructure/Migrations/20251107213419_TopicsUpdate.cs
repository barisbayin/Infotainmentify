using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TopicsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtendedMetadataJson",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "GenerationAttempt",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "MediaRendered",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "ProductionType",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "Published",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "RenderedAt",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "Setting",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "Topics");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Topics",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<string>(
                name: "ScriptHint",
                table: "Topics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Series",
                table: "Topics",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoiceHint",
                table: "Topics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "ScriptHint",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "Series",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "VoiceHint",
                table: "Topics");

            migrationBuilder.AddColumn<string>(
                name: "ExtendedMetadataJson",
                table: "Topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenerationAttempt",
                table: "Topics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MediaRendered",
                table: "Topics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProductionType",
                table: "Topics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "Topics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "Topics",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RenderedAt",
                table: "Topics",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Setting",
                table: "Topics",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "Topics",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
