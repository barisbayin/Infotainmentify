using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutoVideoAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoVideoAssetProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TopicGenerationProfileId = table.Column<int>(type: "int", nullable: false),
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
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    ScriptId = table.Column<int>(type: "int", nullable: true),
                    VideoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Uploaded = table.Column<bool>(type: "bit", nullable: false),
                    UploadVideoId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UploadPlatform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Log = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoVideoAssets");

            migrationBuilder.DropTable(
                name: "AutoVideoAssetProfiles");
        }
    }
}
