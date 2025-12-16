using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeletedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StageConfigs_ContentPipelines_ContentPipelineRun_Id",
                table: "StageConfigs");

            migrationBuilder.DropTable(
                name: "ContentPipelines");

            migrationBuilder.DropTable(
                name: "VideoAssets");

            migrationBuilder.DropTable(
                name: "VideoGenerationProfiles");

            migrationBuilder.DropTable(
                name: "ScriptGenerationProfiles");

            migrationBuilder.DropTable(
                name: "TopicGenerationProfiles");

            migrationBuilder.DropIndex(
                name: "IX_StageConfigs_ContentPipelineRun_Id",
                table: "StageConfigs");

            migrationBuilder.DropColumn(
                name: "ContentPipelineRun_Id",
                table: "StageConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContentPipelineRun_Id",
                table: "StageConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TopicGenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiConnectionId = table.Column<int>(type: "int", nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    PromptId = table.Column<int>(type: "int", nullable: false),
                    AllowRetry = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoGenerateScript = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    MaxTokens = table.Column<int>(type: "int", nullable: true),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OutputMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Topic"),
                    ProductionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    RenderStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: false, defaultValue: 0.7f),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicGenerationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicGenerationProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicGenerationProfiles_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicGenerationProfiles_UserAiConnections_AiConnectionId",
                        column: x => x.AiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VideoAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScriptId = table.Column<int>(type: "int", nullable: false),
                    AssetKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    FilePath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsUploaded = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoAssets_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScriptGenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiConnectionId = table.Column<int>(type: "int", nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ImageAiConnectionId = table.Column<int>(type: "int", nullable: true),
                    PromptId = table.Column<int>(type: "int", nullable: false),
                    SttAiConnectionId = table.Column<int>(type: "int", nullable: true),
                    TopicGenerationProfileId = table.Column<int>(type: "int", nullable: false),
                    TtsAiConnectionId = table.Column<int>(type: "int", nullable: true),
                    VideoAiConnectionId = table.Column<int>(type: "int", nullable: true),
                    AllowRetry = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoGenerateAssets = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AutoRenderVideo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    ImageAspectRatio = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "9:16"),
                    ImageModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageRenderStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OutputMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Script"),
                    ProductionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    RenderStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    SttModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: false, defaultValue: 0.8f),
                    TtsModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TtsVoice = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    VideoModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VideoTemplate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptGenerationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_TopicGenerationProfiles_TopicGenerationProfileId",
                        column: x => x.TopicGenerationProfileId,
                        principalTable: "TopicGenerationProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_UserAiConnections_AiConnectionId",
                        column: x => x.AiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_UserAiConnections_ImageAiConnectionId",
                        column: x => x.ImageAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_UserAiConnections_SttAiConnectionId",
                        column: x => x.SttAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_UserAiConnections_TtsAiConnectionId",
                        column: x => x.TtsAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_UserAiConnections_VideoAiConnectionId",
                        column: x => x.VideoAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoGenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    AutoVideoRenderProfileId = table.Column<int>(type: "int", nullable: true),
                    ScriptGenerationProfileId = table.Column<int>(type: "int", nullable: false),
                    SocialChannelId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    DescriptionTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GenerateThumbnail = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    TitleTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UploadAfterRender = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoGenerationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoGenerationProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoGenerationProfiles_AutoVideoRenderProfiles_AutoVideoRenderProfileId",
                        column: x => x.AutoVideoRenderProfileId,
                        principalTable: "AutoVideoRenderProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoGenerationProfiles_ScriptGenerationProfiles_ScriptGenerationProfileId",
                        column: x => x.ScriptGenerationProfileId,
                        principalTable: "ScriptGenerationProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoGenerationProfiles_SocialChannels_SocialChannelId",
                        column: x => x.SocialChannelId,
                        principalTable: "SocialChannels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentPipelines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    ScriptId = table.Column<int>(type: "int", nullable: true),
                    SocialChannelId = table.Column<int>(type: "int", nullable: true),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    AudioPathsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FinalDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FinalTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImagePathsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LogJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Uploaded = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedPlatform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UploadedVideoId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VideoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPipelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPipelines_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentPipelines_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContentPipelines_SocialChannels_SocialChannelId",
                        column: x => x.SocialChannelId,
                        principalTable: "SocialChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContentPipelines_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContentPipelines_VideoGenerationProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "VideoGenerationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StageConfigs_ContentPipelineRun_Id",
                table: "StageConfigs",
                column: "ContentPipelineRun_Id");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelines_AppUserId",
                table: "ContentPipelines",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelines_CreatedAt",
                table: "ContentPipelines",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelines_ProfileId",
                table: "ContentPipelines",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelines_Removed",
                table: "ContentPipelines",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelines_ScriptId",
                table: "ContentPipelines",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelines_SocialChannelId",
                table: "ContentPipelines",
                column: "SocialChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelines_TopicId",
                table: "ContentPipelines",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_AiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "AiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_AppUserId",
                table: "ScriptGenerationProfiles",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_CreatedAt",
                table: "ScriptGenerationProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_ImageAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "ImageAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_PromptId",
                table: "ScriptGenerationProfiles",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_Removed",
                table: "ScriptGenerationProfiles",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_SttAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "SttAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                column: "TopicGenerationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_TtsAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "TtsAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_VideoAiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "VideoAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_AiConnectionId",
                table: "TopicGenerationProfiles",
                column: "AiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_AppUserId",
                table: "TopicGenerationProfiles",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_CreatedAt",
                table: "TopicGenerationProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_PromptId",
                table: "TopicGenerationProfiles",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_Removed",
                table: "TopicGenerationProfiles",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAssets_CreatedAt",
                table: "VideoAssets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAssets_Removed",
                table: "VideoAssets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAssets_ScriptId_AssetType",
                table: "VideoAssets",
                columns: new[] { "ScriptId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoAssets_UserId",
                table: "VideoAssets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_AppUserId",
                table: "VideoGenerationProfiles",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_AutoVideoRenderProfileId",
                table: "VideoGenerationProfiles",
                column: "AutoVideoRenderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_CreatedAt",
                table: "VideoGenerationProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_Removed",
                table: "VideoGenerationProfiles",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_ScriptGenerationProfileId",
                table: "VideoGenerationProfiles",
                column: "ScriptGenerationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_SocialChannelId",
                table: "VideoGenerationProfiles",
                column: "SocialChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_StageConfigs_ContentPipelines_ContentPipelineRun_Id",
                table: "StageConfigs",
                column: "ContentPipelineRun_Id",
                principalTable: "ContentPipelines",
                principalColumn: "Id");
        }
    }
}
