using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScriptGenerationUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_AppUsers_UserId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "RawResponseJson",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "TopicIdsJson",
                table: "ScriptGenerationProfiles");

            migrationBuilder.AddColumn<bool>(
                name: "AllowScriptGeneration",
                table: "Topics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Scripts",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "Scripts",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "Scripts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Scripts",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Scripts",
                type: "datetime2(0)",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "AiConnectionId",
                table: "Scripts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionType",
                table: "Scripts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptId",
                table: "Scripts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RenderStyle",
                table: "Scripts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTimeMs",
                table: "Scripts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScriptGenerationProfileId",
                table: "Scripts",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Temperature",
                table: "ScriptGenerationProfiles",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float",
                oldDefaultValue: 0.80000000000000004);

            migrationBuilder.AlterColumn<string>(
                name: "RenderStyle",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductionType",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true,
                oldDefaultValue: "en");

            migrationBuilder.AddColumn<bool>(
                name: "AllowRetry",
                table: "ScriptGenerationProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "ScriptGenerationProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OutputMode",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Script");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_AiConnectionId",
                table: "Scripts",
                column: "AiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_CreatedAt",
                table: "Scripts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_PromptId",
                table: "Scripts",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_Removed",
                table: "Scripts",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_ScriptGenerationProfileId",
                table: "Scripts",
                column: "ScriptGenerationProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_AppUsers_UserId",
                table: "Scripts",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_Prompts_PromptId",
                table: "Scripts",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "Scripts",
                column: "ScriptGenerationProfileId",
                principalTable: "ScriptGenerationProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_UserAiConnections_AiConnectionId",
                table: "Scripts",
                column: "AiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_AppUsers_UserId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_Prompts_PromptId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_UserAiConnections_AiConnectionId",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_AiConnectionId",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_CreatedAt",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_PromptId",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_Removed",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_ScriptGenerationProfileId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "AllowScriptGeneration",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "AiConnectionId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "ProductionType",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "PromptId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "RenderStyle",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "ResponseTimeMs",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "ScriptGenerationProfileId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "AllowRetry",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "OutputMode",
                table: "ScriptGenerationProfiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Scripts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "Scripts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "Scripts",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Scripts",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Scripts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<double>(
                name: "Temperature",
                table: "ScriptGenerationProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.80000000000000004,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<string>(
                name: "RenderStyle",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductionType",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                defaultValue: "en",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValue: "en");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "ScriptGenerationProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawResponseJson",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "ScriptGenerationProfiles",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "TopicIdsJson",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_AppUsers_UserId",
                table: "Scripts",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
