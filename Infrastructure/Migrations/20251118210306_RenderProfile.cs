using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenderProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_AppUsers_AppUserId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_UserSocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles");

            migrationBuilder.AddColumn<int>(
                name: "AutoVideoRenderProfileId",
                table: "VideoGenerationProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SttAiConnectionId",
                table: "ScriptGenerationProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SttModelName",
                table: "ScriptGenerationProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AutoVideoRenderProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Fps = table.Column<int>(type: "int", nullable: false),
                    Style = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CaptionStyle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CaptionFont = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CaptionSize = table.Column<int>(type: "int", nullable: false),
                    CaptionGlow = table.Column<bool>(type: "bit", nullable: false),
                    CaptionKaraoke = table.Column<bool>(type: "bit", nullable: false),
                    ZoomSpeed = table.Column<double>(type: "float", nullable: false),
                    ZoomMax = table.Column<double>(type: "float", nullable: false),
                    PanX = table.Column<double>(type: "float", nullable: false),
                    PanY = table.Column<double>(type: "float", nullable: false),
                    Transition = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TransitionDuration = table.Column<double>(type: "float", nullable: false),
                    TimelineMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BgmVolume = table.Column<int>(type: "int", nullable: false),
                    VoiceVolume = table.Column<int>(type: "int", nullable: false),
                    DuckingStrength = table.Column<int>(type: "int", nullable: false),
                    AiRecommendedStyle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AiRecommendedTransitions = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AiRecommendedCaption = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoVideoRenderProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoVideoRenderProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_AutoVideoRenderProfileId",
                table: "VideoGenerationProfiles",
                column: "AutoVideoRenderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_SttAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "SttAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoRenderProfiles_AppUserId",
                table: "AutoVideoRenderProfiles",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_SttAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "SttAiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_AppUsers_AppUserId",
                table: "VideoGenerationProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_AutoVideoRenderProfiles_AutoVideoRenderProfileId",
                table: "VideoGenerationProfiles",
                column: "AutoVideoRenderProfileId",
                principalTable: "AutoVideoRenderProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "VideoGenerationProfiles",
                column: "ScriptGenerationProfileId",
                principalTable: "ScriptGenerationProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_UserSocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles",
                column: "SocialChannelId",
                principalTable: "UserSocialChannels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScriptGenerationProfiles_UserAiConnections_SttAiConnectionId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_AppUsers_AppUserId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_AutoVideoRenderProfiles_AutoVideoRenderProfileId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_UserSocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropTable(
                name: "AutoVideoRenderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_VideoGenerationProfiles_AutoVideoRenderProfileId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ScriptGenerationProfiles_SttAiConnectionId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "AutoVideoRenderProfileId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "SttAiConnectionId",
                table: "ScriptGenerationProfiles");

            migrationBuilder.DropColumn(
                name: "SttModelName",
                table: "ScriptGenerationProfiles");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_AppUsers_AppUserId",
                table: "VideoGenerationProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_ScriptGenerationProfiles_ScriptGenerationProfileId",
                table: "VideoGenerationProfiles",
                column: "ScriptGenerationProfileId",
                principalTable: "ScriptGenerationProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_UserSocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles",
                column: "SocialChannelId",
                principalTable: "UserSocialChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
