using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BigChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoVideoRenderProfiles_AppUsers_AppUserId",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_JobExecutions_JobSettings_JobId",
                table: "JobExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSettings_AppUsers_AppUserId",
                table: "JobSettings");

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
                name: "FK_Scripts_Topics_TopicId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_UserAiConnections_AiConnectionId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_TopicGenerationProfiles_AppUsers_AppUserId",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_Prompts_PromptId",
                table: "Topics");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_Scripts_ScriptId",
                table: "Topics");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAiConnections_AppUsers_UserId",
                table: "UserAiConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_UserSocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropTable(
                name: "AutoVideoAssetFile");

            migrationBuilder.DropTable(
                name: "AutoVideoPipeline");

            migrationBuilder.DropTable(
                name: "UserSocialChannels");

            migrationBuilder.DropIndex(
                name: "IX_Topics_PromptId",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Topics_ScriptId",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Topics_TopicCode",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_AiConnectionId",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_PromptId",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_ScriptGenerationProfileId",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_UserId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "CredentialFilePath",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "EncryptedCredentialJson",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "ImageModel",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "TextModel",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "AllowScriptGeneration",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "FactCheck",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "NeedsFootage",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "PotentialVisual",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "ScriptGenerated",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "ScriptGeneratedAt",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "ScriptHint",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "TopicCode",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "VoiceHint",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "AiConnectionId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "MetaJson",
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
                name: "Summary",
                table: "Scripts");

            migrationBuilder.RenameColumn(
                name: "VideoModel",
                table: "UserAiConnections",
                newName: "ExtraId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserAiConnections",
                newName: "AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAiConnections_UserId_Name",
                table: "UserAiConnections",
                newName: "IX_UserAiConnections_AppUserId_Name");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Topics",
                newName: "AppUserId");

            migrationBuilder.RenameColumn(
                name: "TopicJson",
                table: "Topics",
                newName: "TagsJson");

            migrationBuilder.RenameColumn(
                name: "ScriptId",
                table: "Topics",
                newName: "SourcePresetId");

            migrationBuilder.RenameColumn(
                name: "PromptId",
                table: "Topics",
                newName: "CreatedByRunId");

            migrationBuilder.RenameColumn(
                name: "PremiseTr",
                table: "Topics",
                newName: "RawJsonData");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Scripts",
                newName: "EstimatedDurationSec");

            migrationBuilder.RenameColumn(
                name: "ScriptJson",
                table: "Scripts",
                newName: "ScenesJson");

            migrationBuilder.RenameColumn(
                name: "ScriptGenerationProfileId",
                table: "Scripts",
                newName: "SourcePresetId");

            migrationBuilder.RenameColumn(
                name: "ResponseTimeMs",
                table: "Scripts",
                newName: "CreatedByRunId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserAiConnections",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "UserAiConnections",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "UserAiConnections",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "UserAiConnections",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserAiConnections",
                type: "datetime2(0)",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedApiKey",
                table: "UserAiConnections",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Tone",
                table: "Topics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Premise",
                table: "Topics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Topics",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Topics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisualPromptHint",
                table: "Topics",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TopicId",
                table: "Scripts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "Scripts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Scripts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "AppUsers",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "AppUsers",
                type: "datetime2(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AppUsers",
                type: "datetime2(0)",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateTable(
                name: "Concepts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concepts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Concepts_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagePresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    UserAiConnectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArtStyle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Size = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PromptTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    NegativePrompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageCountPerScene = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagePresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagePresets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImagePresets_UserAiConnections_UserAiConnectionId",
                        column: x => x.UserAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RenderPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OutputWidth = table.Column<int>(type: "int", nullable: false),
                    OutputHeight = table.Column<int>(type: "int", nullable: false),
                    Fps = table.Column<int>(type: "int", nullable: false),
                    BitrateKbps = table.Column<int>(type: "int", nullable: false),
                    EncoderPreset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContainerFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CaptionSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioMixSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisualEffectsSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandingSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenderPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RenderPresets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScriptPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    UserAiConnectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetDurationSec = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IncludeHook = table.Column<bool>(type: "bit", nullable: false),
                    IncludeCta = table.Column<bool>(type: "bit", nullable: false),
                    PromptTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    SystemInstruction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScriptPresets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScriptPresets_UserAiConnections_UserAiConnectionId",
                        column: x => x.UserAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ChannelType = table.Column<int>(type: "int", nullable: false),
                    ChannelName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChannelHandle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChannelUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlatformChannelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EncryptedTokensJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialChannels_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SttPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    UserAiConnectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EnableWordLevelTimestamps = table.Column<bool>(type: "bit", nullable: false),
                    EnableSpeakerDiarization = table.Column<bool>(type: "bit", nullable: false),
                    OutputFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    FilterProfanity = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SttPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SttPresets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SttPresets_UserAiConnections_UserAiConnectionId",
                        column: x => x.UserAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TopicPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserAiConnectionId = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PromptTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ContextKeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemInstruction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicPresets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicPresets_UserAiConnections_UserAiConnectionId",
                        column: x => x.UserAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TtsPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    UserAiConnectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VoiceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EngineModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpeakingRate = table.Column<double>(type: "float", nullable: false),
                    Pitch = table.Column<double>(type: "float", nullable: false),
                    Stability = table.Column<double>(type: "float", nullable: false),
                    Clarity = table.Column<double>(type: "float", nullable: false),
                    StyleExaggeration = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TtsPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TtsPresets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TtsPresets_UserAiConnections_UserAiConnectionId",
                        column: x => x.UserAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VideoPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    UserAiConnectionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GenerationMode = table.Column<int>(type: "int", nullable: false),
                    AspectRatio = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    PromptTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    NegativePrompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CameraControlSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdvancedSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoPresets_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoPresets_UserAiConnections_UserAiConnectionId",
                        column: x => x.UserAiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentPipelineTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ConceptId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPipelineTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPipelineTemplates_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentPipelineTemplates_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentPipelines",
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

            migrationBuilder.CreateTable(
                name: "ContentPipelineRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPipelineRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPipelineRuns_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentPipelineRuns_ContentPipelineTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ContentPipelineTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StageConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentPipelineTemplateId = table.Column<int>(type: "int", nullable: false),
                    StageType = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    PresetId = table.Column<int>(type: "int", nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentPipelineRun_Id = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageConfigs_ContentPipelineTemplates_ContentPipelineTemplateId",
                        column: x => x.ContentPipelineTemplateId,
                        principalTable: "ContentPipelineTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StageConfigs_ContentPipelines_ContentPipelineRun_Id",
                        column: x => x.ContentPipelineRun_Id,
                        principalTable: "ContentPipelines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StageExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentPipelineRunId = table.Column<int>(type: "int", nullable: false),
                    StageConfigId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    CpuTimeMs = table.Column<int>(type: "int", nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageExecutions_ContentPipelineRuns_ContentPipelineRunId",
                        column: x => x.ContentPipelineRunId,
                        principalTable: "ContentPipelineRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StageExecutions_StageConfigs_StageConfigId",
                        column: x => x.StageConfigId,
                        principalTable: "StageConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAiConnections_CreatedAt",
                table: "UserAiConnections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserAiConnections_Removed",
                table: "UserAiConnections",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_AppUserId",
                table: "Topics",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Category",
                table: "Topics",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_AppUserId",
                table: "Scripts",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_CreatedAt",
                table: "AppUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Removed",
                table: "AppUsers",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_AppUserId_Name",
                table: "Concepts",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_CreatedAt",
                table: "Concepts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_Removed",
                table: "Concepts",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelineRuns_AppUserId_Status",
                table: "ContentPipelineRuns",
                columns: new[] { "AppUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelineRuns_CreatedAt",
                table: "ContentPipelineRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelineRuns_Removed",
                table: "ContentPipelineRuns",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelineRuns_TemplateId",
                table: "ContentPipelineRuns",
                column: "TemplateId");

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
                name: "IX_ContentPipelineTemplates_AppUserId",
                table: "ContentPipelineTemplates",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelineTemplates_ConceptId_Name",
                table: "ContentPipelineTemplates",
                columns: new[] { "ConceptId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelineTemplates_CreatedAt",
                table: "ContentPipelineTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPipelineTemplates_Removed",
                table: "ContentPipelineTemplates",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ImagePresets_AppUserId_Name",
                table: "ImagePresets",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagePresets_CreatedAt",
                table: "ImagePresets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImagePresets_Removed",
                table: "ImagePresets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ImagePresets_UserAiConnectionId",
                table: "ImagePresets",
                column: "UserAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderPresets_AppUserId_Name",
                table: "RenderPresets",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RenderPresets_CreatedAt",
                table: "RenderPresets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RenderPresets_Removed",
                table: "RenderPresets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptPresets_AppUserId_Name",
                table: "ScriptPresets",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScriptPresets_CreatedAt",
                table: "ScriptPresets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptPresets_Removed",
                table: "ScriptPresets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptPresets_UserAiConnectionId",
                table: "ScriptPresets",
                column: "UserAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialChannels_AppUserId_PlatformChannelId",
                table: "SocialChannels",
                columns: new[] { "AppUserId", "PlatformChannelId" },
                unique: true,
                filter: "[PlatformChannelId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SocialChannels_CreatedAt",
                table: "SocialChannels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SocialChannels_Removed",
                table: "SocialChannels",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_StageConfigs_ContentPipelineRun_Id",
                table: "StageConfigs",
                column: "ContentPipelineRun_Id");

            migrationBuilder.CreateIndex(
                name: "IX_StageConfigs_ContentPipelineTemplateId_Order",
                table: "StageConfigs",
                columns: new[] { "ContentPipelineTemplateId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StageConfigs_CreatedAt",
                table: "StageConfigs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StageConfigs_Removed",
                table: "StageConfigs",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_ContentPipelineRunId_Status",
                table: "StageExecutions",
                columns: new[] { "ContentPipelineRunId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_CreatedAt",
                table: "StageExecutions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_Removed",
                table: "StageExecutions",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_StageExecutions_StageConfigId_Status",
                table: "StageExecutions",
                columns: new[] { "StageConfigId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SttPresets_AppUserId_Name",
                table: "SttPresets",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SttPresets_CreatedAt",
                table: "SttPresets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SttPresets_Removed",
                table: "SttPresets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_SttPresets_UserAiConnectionId",
                table: "SttPresets",
                column: "UserAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicPresets_AppUserId_Name",
                table: "TopicPresets",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopicPresets_CreatedAt",
                table: "TopicPresets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TopicPresets_Removed",
                table: "TopicPresets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_TopicPresets_UserAiConnectionId",
                table: "TopicPresets",
                column: "UserAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TtsPresets_AppUserId_Name",
                table: "TtsPresets",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TtsPresets_CreatedAt",
                table: "TtsPresets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TtsPresets_Removed",
                table: "TtsPresets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_TtsPresets_UserAiConnectionId",
                table: "TtsPresets",
                column: "UserAiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoPresets_AppUserId_Name",
                table: "VideoPresets",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoPresets_CreatedAt",
                table: "VideoPresets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoPresets_Removed",
                table: "VideoPresets",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_VideoPresets_UserAiConnectionId",
                table: "VideoPresets",
                column: "UserAiConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoVideoRenderProfiles_AppUsers_AppUserId",
                table: "AutoVideoRenderProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobExecutions_JobSettings_JobId",
                table: "JobExecutions",
                column: "JobId",
                principalTable: "JobSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSettings_AppUsers_AppUserId",
                table: "JobSettings",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_AppUsers_AppUserId",
                table: "Scripts",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_Topics_TopicId",
                table: "Scripts",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TopicGenerationProfiles_AppUsers_AppUserId",
                table: "TopicGenerationProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_AppUsers_AppUserId",
                table: "Topics",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAiConnections_AppUsers_AppUserId",
                table: "UserAiConnections",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_SocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles",
                column: "SocialChannelId",
                principalTable: "SocialChannels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoVideoRenderProfiles_AppUsers_AppUserId",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_JobExecutions_JobSettings_JobId",
                table: "JobExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSettings_AppUsers_AppUserId",
                table: "JobSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_AppUsers_AppUserId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_Topics_TopicId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_TopicGenerationProfiles_AppUsers_AppUserId",
                table: "TopicGenerationProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_AppUsers_AppUserId",
                table: "Topics");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAiConnections_AppUsers_AppUserId",
                table: "UserAiConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoGenerationProfiles_SocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles");

            migrationBuilder.DropTable(
                name: "ImagePresets");

            migrationBuilder.DropTable(
                name: "RenderPresets");

            migrationBuilder.DropTable(
                name: "ScriptPresets");

            migrationBuilder.DropTable(
                name: "StageExecutions");

            migrationBuilder.DropTable(
                name: "SttPresets");

            migrationBuilder.DropTable(
                name: "TopicPresets");

            migrationBuilder.DropTable(
                name: "TtsPresets");

            migrationBuilder.DropTable(
                name: "VideoPresets");

            migrationBuilder.DropTable(
                name: "ContentPipelineRuns");

            migrationBuilder.DropTable(
                name: "StageConfigs");

            migrationBuilder.DropTable(
                name: "ContentPipelineTemplates");

            migrationBuilder.DropTable(
                name: "ContentPipelines");

            migrationBuilder.DropTable(
                name: "Concepts");

            migrationBuilder.DropTable(
                name: "SocialChannels");

            migrationBuilder.DropIndex(
                name: "IX_UserAiConnections_CreatedAt",
                table: "UserAiConnections");

            migrationBuilder.DropIndex(
                name: "IX_UserAiConnections_Removed",
                table: "UserAiConnections");

            migrationBuilder.DropIndex(
                name: "IX_Topics_AppUserId",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Topics_Category",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Scripts_AppUserId",
                table: "Scripts");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_CreatedAt",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_Removed",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "EncryptedApiKey",
                table: "UserAiConnections");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "VisualPromptHint",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Scripts");

            migrationBuilder.RenameColumn(
                name: "ExtraId",
                table: "UserAiConnections",
                newName: "VideoModel");

            migrationBuilder.RenameColumn(
                name: "AppUserId",
                table: "UserAiConnections",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAiConnections_AppUserId_Name",
                table: "UserAiConnections",
                newName: "IX_UserAiConnections_UserId_Name");

            migrationBuilder.RenameColumn(
                name: "TagsJson",
                table: "Topics",
                newName: "TopicJson");

            migrationBuilder.RenameColumn(
                name: "SourcePresetId",
                table: "Topics",
                newName: "ScriptId");

            migrationBuilder.RenameColumn(
                name: "RawJsonData",
                table: "Topics",
                newName: "PremiseTr");

            migrationBuilder.RenameColumn(
                name: "CreatedByRunId",
                table: "Topics",
                newName: "PromptId");

            migrationBuilder.RenameColumn(
                name: "AppUserId",
                table: "Topics",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "SourcePresetId",
                table: "Scripts",
                newName: "ScriptGenerationProfileId");

            migrationBuilder.RenameColumn(
                name: "ScenesJson",
                table: "Scripts",
                newName: "ScriptJson");

            migrationBuilder.RenameColumn(
                name: "EstimatedDurationSec",
                table: "Scripts",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CreatedByRunId",
                table: "Scripts",
                newName: "ResponseTimeMs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserAiConnections",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "UserAiConnections",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "UserAiConnections",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "UserAiConnections",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserAiConnections",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CredentialFilePath",
                table: "UserAiConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedCredentialJson",
                table: "UserAiConnections",
                type: "nvarchar(max)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageModel",
                table: "UserAiConnections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "UserAiConnections",
                type: "decimal(3,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextModel",
                table: "UserAiConnections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tone",
                table: "Topics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Premise",
                table: "Topics",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "AllowScriptGeneration",
                table: "Topics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "FactCheck",
                table: "Topics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsFootage",
                table: "Topics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PotentialVisual",
                table: "Topics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Topics",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<bool>(
                name: "ScriptGenerated",
                table: "Topics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScriptGeneratedAt",
                table: "Topics",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScriptHint",
                table: "Topics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopicCode",
                table: "Topics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoiceHint",
                table: "Topics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TopicId",
                table: "Scripts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiConnectionId",
                table: "Scripts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Scripts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaJson",
                table: "Scripts",
                type: "nvarchar(max)",
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

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Scripts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "AppUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RemovedAt",
                table: "AppUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Removed",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AppUsers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.CreateTable(
                name: "UserSocialChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ChannelHandle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChannelName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChannelType = table.Column<int>(type: "int", nullable: false),
                    ChannelUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EncryptedTokensJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PlatformChannelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", maxLength: 1000, nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSocialChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSocialChannels_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutoVideoPipeline",
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
                    AssetKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    SceneNumber = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
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
                name: "IX_Topics_PromptId",
                table: "Topics",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_ScriptId",
                table: "Topics",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_TopicCode",
                table: "Topics",
                column: "TopicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_AiConnectionId",
                table: "Scripts",
                column: "AiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_PromptId",
                table: "Scripts",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_ScriptGenerationProfileId",
                table: "Scripts",
                column: "ScriptGenerationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_UserId",
                table: "Scripts",
                column: "UserId");

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
                name: "IX_UserSocialChannels_AppUserId_ChannelType",
                table: "UserSocialChannels",
                columns: new[] { "AppUserId", "ChannelType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AutoVideoRenderProfiles_AppUsers_AppUserId",
                table: "AutoVideoRenderProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobExecutions_JobSettings_JobId",
                table: "JobExecutions",
                column: "JobId",
                principalTable: "JobSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSettings_AppUsers_AppUserId",
                table: "JobSettings",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_Scripts_Topics_TopicId",
                table: "Scripts",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_UserAiConnections_AiConnectionId",
                table: "Scripts",
                column: "AiConnectionId",
                principalTable: "UserAiConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TopicGenerationProfiles_AppUsers_AppUserId",
                table: "TopicGenerationProfiles",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_Prompts_PromptId",
                table: "Topics",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_Scripts_ScriptId",
                table: "Topics",
                column: "ScriptId",
                principalTable: "Scripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAiConnections_AppUsers_UserId",
                table: "UserAiConnections",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoGenerationProfiles_UserSocialChannels_SocialChannelId",
                table: "VideoGenerationProfiles",
                column: "SocialChannelId",
                principalTable: "UserSocialChannels",
                principalColumn: "Id");
        }
    }
}
