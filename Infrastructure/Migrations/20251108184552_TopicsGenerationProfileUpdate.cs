using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TopicsGenerationProfileUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TopicGenerationProfiles_StartedAt",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TopicGenerationProfiles_Status",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "RawResponseJson",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TopicGenerationProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "RequestedCount",
                table: "TopicGenerationProfiles",
                type: "int",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RenderStyle",
                table: "TopicGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfileName",
                table: "TopicGenerationProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ProductionType",
                table: "TopicGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "TopicGenerationProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "AllowRetry",
                table: "TopicGenerationProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoGenerateScript",
                table: "TopicGenerationProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "TopicGenerationProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "TopicGenerationProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "TopicGenerationProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputMode",
                table: "TopicGenerationProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Topic");

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "TopicGenerationProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Temperature",
                table: "TopicGenerationProfiles",
                type: "real",
                nullable: false,
                defaultValue: 0.7f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowRetry",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "AutoGenerateScript",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "OutputMode",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "TopicGenerationProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "RequestedCount",
                table: "TopicGenerationProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 30);

            migrationBuilder.AlterColumn<string>(
                name: "RenderStyle",
                table: "TopicGenerationProfiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfileName",
                table: "TopicGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ProductionType",
                table: "TopicGenerationProfiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "TopicGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "TopicGenerationProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawResponseJson",
                table: "TopicGenerationProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "TopicGenerationProfiles",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TopicGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_StartedAt",
                table: "TopicGenerationProfiles",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_Status",
                table: "TopicGenerationProfiles",
                column: "Status");
        }
    }
}
