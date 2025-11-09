using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScriptGenerationUpdateNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfiles_AppUsers_AppUserId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfiles_Prompts_PromptId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfiles_TopicGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_AiConnectionId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "Scripts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScriptGenerationProfiles",
                table: "ScriptGenerationProfiles");

            migrationBuilder.RenameTable(
                name: "ScriptGenerationProfiles",
                newName: "ScriptGenerationProfile");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_TopicGenerationProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_PromptId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_PromptId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_AppUserId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_AiConnectionId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_AiConnectionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ScriptGenerationProfile",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Temperature",
                table: "ScriptGenerationProfile",
                type: "real",
                nullable: false,
                defaultValue: 0.8f,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "ScriptGenerationProfile",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "ScriptGenerationProfile",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ScriptGenerationProfile",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ScriptGenerationProfile",
                type: "datetime2(0)",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "AutoGenerateAssets",
                table: "ScriptGenerationProfile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenderVideo",
                table: "ScriptGenerationProfile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ImageAiConnectionId",
                table: "ScriptGenerationProfile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageAspectRatio",
                table: "ScriptGenerationProfile",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "9:16");

            migrationBuilder.AddColumn<string>(
                name: "ImageModelName",
                table: "ScriptGenerationProfile",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageRenderStyle",
                table: "ScriptGenerationProfile",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TtsAiConnectionId",
                table: "ScriptGenerationProfile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TtsModelName",
                table: "ScriptGenerationProfile",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TtsVoice",
                table: "ScriptGenerationProfile",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoAiConnectionId",
                table: "ScriptGenerationProfile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoModelName",
                table: "ScriptGenerationProfile",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoTemplate",
                table: "ScriptGenerationProfile",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScriptGenerationProfile",
                table: "ScriptGenerationProfile",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfile_CreatedAt",
                table: "ScriptGenerationProfile",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfile_ImageAiConnectionId",
                table: "ScriptGenerationProfile",
                column: "ImageAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfile_Removed",
                table: "ScriptGenerationProfile",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfile_TtsAiConnectionId",
                table: "ScriptGenerationProfile",
                column: "TtsAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfile_VideoAiConnectionId",
                table: "ScriptGenerationProfile",
                column: "VideoAiConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfile_AppUsers_AppUserId",
                table: "ScriptGenerationProfile",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfile_Prompts_PromptId",
                table: "ScriptGenerationProfile",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfile_TopicGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfile",
                column: "TopicGenerationProfileId",
                principalTable: "TopicGenerationProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_AiConnectionId",
                table: "ScriptGenerationProfile",
                column: "AiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_ImageAiConnectionId",
                table: "ScriptGenerationProfile",
                column: "ImageAiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_TtsAiConnectionId",
                table: "ScriptGenerationProfile",
                column: "TtsAiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_VideoAiConnectionId",
                table: "ScriptGenerationProfile",
                column: "VideoAiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_ScriptGenerationProfile_ScriptGenerationProfileId",
                table: "Scripts",
                column: "ScriptGenerationProfileId",
                principalTable: "ScriptGenerationProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfile_AppUsers_AppUserId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfile_Prompts_PromptId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfile_TopicGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_AiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_ImageAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_TtsAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfile_UserAiConnections_VideoAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_ScriptGenerationProfile_ScriptGenerationProfileId",
                table: "Scripts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScriptGenerationProfile",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropIndex(
                name: "IX_ScriptGenerationProfile_CreatedAt",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropIndex(
                name: "IX_ScriptGenerationProfile_ImageAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropIndex(
                name: "IX_ScriptGenerationProfile_Removed",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropIndex(
                name: "IX_ScriptGenerationProfile_TtsAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropIndex(
                name: "IX_ScriptGenerationProfile_VideoAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "AutoGenerateAssets",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "AutoRenderVideo",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "ImageAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "ImageAspectRatio",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "ImageModelName",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "ImageRenderStyle",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "TtsAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "TtsModelName",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "TtsVoice",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "VideoAiConnectionId",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "VideoModelName",
                table: "ScriptGenerationProfile");

            migrationBuilder.DropColumn(
                name: "VideoTemplate",
                table: "ScriptGenerationProfile");

            migrationBuilder.RenameTable(
                name: "ScriptGenerationProfile",
                newName: "ScriptGenerationProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_TopicGenerationProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_PromptId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_PromptId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_AppUserId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_AiConnectionId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_AiConnectionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ScriptGenerationProfiles",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Temperature",
                table: "ScriptGenerationProfiles",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real",
                oldDefaultValue: 0.8f);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "ScriptGenerationProfiles",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "ScriptGenerationProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ScriptGenerationProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ScriptGenerationProfiles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScriptGenerationProfiles",
                table: "ScriptGenerationProfiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_AppUsers_AppUserId",
                table: "ScriptGenerationProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_Prompts_PromptId",
                table: "ScriptGenerationProfiles",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_TopicGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                column: "TopicGenerationProfileId",
                principalTable: "TopicGenerationProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_AiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "AiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "Scripts",
                column: "ScriptGenerationProfileId",
                principalTable: "ScriptGenerationProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
