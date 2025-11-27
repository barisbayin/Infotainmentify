using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutoAssetFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoVideoAssets");

            migrationBuilder.DropTable(
                name: "AutoVideoAssetProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "VideoGenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScriptGenerationProfileId = table.Column<int>(type: "int", nullable: false),
                    SocialChannelId = table.Column<int>(type: "int", nullable: true),
                    UploadAfterRender = table.Column<bool>(type: "bit", nullable: false),
                    GenerateThumbnail = table.Column<bool>(type: "bit", nullable: false),
                    TitleTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoGenerationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoGenerationProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoGenerationProfiles_ScriptGenerationProfiles_ScriptGenerationProfileId",
                        column: x => x.ScriptGenerationProfileId,
                        principalTable: "ScriptGenerationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoGenerationProfiles_UserSocialChannels_SocialChannelId",
                        column: x => x.SocialChannelId,
                        principalTable: "UserSocialChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AutoVideoPipeline",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    ScriptId = table.Column<int>(type: "int", nullable: true),
                    ImagePathsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AudioPathsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    VideoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FinalTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FinalDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SocialChannelId = table.Column<int>(type: "int", nullable: true),
                    UploadedVideoId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UploadedPlatform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Uploaded = table.Column<bool>(type: "bit", nullable: false),
                    LogJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoVideoPipeline", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoVideoPipeline_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoVideoPipeline_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AutoVideoPipeline_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AutoVideoPipeline_UserSocialChannels_SocialChannelId",
                        column: x => x.SocialChannelId,
                        principalTable: "UserSocialChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AutoVideoPipeline_VideoGenerationProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "VideoGenerationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutoVideoAssetFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    AutoVideoPipelineId = table.Column<int>(type: "int", nullable: false),
                    SceneNumber = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    AssetKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoVideoAssetFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoVideoAssetFile_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoVideoAssetFile_AutoVideoPipeline_AutoVideoPipelineId",
                        column: x => x.AutoVideoPipelineId,
                        principalTable: "AutoVideoPipeline",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetFile_AppUserId",
                table: "AutoVideoAssetFile",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetFile_AutoVideoPipelineId",
                table: "AutoVideoAssetFile",
                column: "AutoVideoPipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetFile_CreatedAt",
                table: "AutoVideoAssetFile",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetFile_Removed",
                table: "AutoVideoAssetFile",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoPipeline_AppUserId",
                table: "AutoVideoPipeline",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoPipeline_CreatedAt",
                table: "AutoVideoPipeline",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoPipeline_ProfileId",
                table: "AutoVideoPipeline",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoPipeline_Removed",
                table: "AutoVideoPipeline",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoPipeline_ScriptId",
                table: "AutoVideoPipeline",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoPipeline_SocialChannelId",
                table: "AutoVideoPipeline",
                column: "SocialChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoPipeline_TopicId",
                table: "AutoVideoPipeline",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoGenerationProfiles_AppUserId",
                table: "VideoGenerationProfiles",
                column: "AppUserId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoVideoAssetFile");

            migrationBuilder.DropTable(
                name: "AutoVideoPipeline");

            migrationBuilder.DropTable(
                name: "VideoGenerationProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "AutoVideoAssetProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ScriptGenerationProfileId = table.Column<int>(type: "int", nullable: false),
                    SocialChannelId = table.Column<int>(type: "int", nullable: true),
                    TopicGenerationProfileId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AutoVideoAssetProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoVideoAssetProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutoVideoAssetProfiles_ScriptGenerationProfiles_ScriptGenerationProfileId",
                        column: x => x.ScriptGenerationProfileId,
                        principalTable: "ScriptGenerationProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutoVideoAssetProfiles_TopicGenerationProfiles_TopicGenerationProfileId",
                        column: x => x.TopicGenerationProfileId,
                        principalTable: "TopicGenerationProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutoVideoAssetProfiles_UserSocialChannels_SocialChannelId",
                        column: x => x.SocialChannelId,
                        principalTable: "UserSocialChannels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AutoVideoAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Log = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ScriptId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UploadPlatform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UploadVideoId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Uploaded = table.Column<bool>(type: "bit", nullable: false),
                    VideoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoVideoAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoVideoAssets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutoVideoAssets_AutoVideoAssetProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "AutoVideoAssetProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutoVideoAssets_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutoVideoAssets_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetProfiles_AppUserId",
                table: "AutoVideoAssetProfiles",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetProfiles_AppUserId_ProfileName",
                table: "AutoVideoAssetProfiles",
                columns: new[] { "AppUserId", "ProfileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetProfiles_CreatedAt",
                table: "AutoVideoAssetProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetProfiles_Removed",
                table: "AutoVideoAssetProfiles",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetProfiles_ScriptGenerationProfileId",
                table: "AutoVideoAssetProfiles",
                column: "ScriptGenerationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetProfiles_SocialChannelId",
                table: "AutoVideoAssetProfiles",
                column: "SocialChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssetProfiles_TopicGenerationProfileId",
                table: "AutoVideoAssetProfiles",
                column: "TopicGenerationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_AppUserId",
                table: "AutoVideoAssets",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_CreatedAt",
                table: "AutoVideoAssets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_ProfileId",
                table: "AutoVideoAssets",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_Removed",
                table: "AutoVideoAssets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_ScriptId",
                table: "AutoVideoAssets",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_Status",
                table: "AutoVideoAssets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_TopicId",
                table: "AutoVideoAssets",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_UploadPlatform",
                table: "AutoVideoAssets",
                column: "UploadPlatform");

            migrationBuilder.CreateIndex(
                name: "IX_AutoVideoAssets_UploadVideoId",
                table: "AutoVideoAssets",
                column: "UploadVideoId");
        }
    }
}
