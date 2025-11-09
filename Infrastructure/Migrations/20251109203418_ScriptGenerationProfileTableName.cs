using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScriptGenerationProfileTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameTable(
                name: "ScriptGenerationProfile",
                newName: "ScriptGenerationProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_VideoAiConnectionId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_VideoAiConnectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_TtsAiConnectionId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_TtsAiConnectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_TopicGenerationProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_Removed",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_Removed");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_PromptId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_PromptId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_ImageAiConnectionId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_ImageAiConnectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_CreatedAt",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_AppUserId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfile_AiConnectionId",
                table: "ScriptGenerationProfiles",
                newName: "IX_ScriptGenerationProfiles_AiConnectionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScriptGenerationProfiles",
                table: "ScriptGenerationProfiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_AppUsers_AppUserId",
                table: "ScriptGenerationProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_Prompts_PromptId",
                table: "ScriptGenerationProfiles",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_TopicGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                column: "TopicGenerationProfileId",
                principalTable: "TopicGenerationProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_AiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "AiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_ImageAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "ImageAiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_TtsAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "TtsAiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_VideoAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "VideoAiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "Scripts",
                column: "ScriptGenerationProfileId",
                principalTable: "ScriptGenerationProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "FK_ScriptGenerationProfiles_UserAiConnections_ImageAiConnectionId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_TtsAiConnectionId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_VideoAiConnectionId",
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
                name: "IX_ScriptGenerationProfiles_VideoAiConnectionId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_VideoAiConnectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_TtsAiConnectionId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_TtsAiConnectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_TopicGenerationProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_Removed",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_Removed");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_PromptId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_PromptId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_ImageAiConnectionId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_ImageAiConnectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_CreatedAt",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_AppUserId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ScriptGenerationProfiles_AiConnectionId",
                table: "ScriptGenerationProfile",
                newName: "IX_ScriptGenerationProfile_AiConnectionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScriptGenerationProfile",
                table: "ScriptGenerationProfile",
                column: "Id");

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
    }
}
