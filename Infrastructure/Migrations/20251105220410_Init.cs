using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    DirectoryName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    ProfileType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsAutoRunEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PeriodHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    LastRunAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastErrorAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSettings_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAiConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    TextModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ImageModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VideoModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    EncryptedCredentialJson = table.Column<string>(type: "nvarchar(max)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAiConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAiConnections_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSocialChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ChannelType = table.Column<int>(type: "int", nullable: false),
                    ChannelName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChannelHandle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChannelUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlatformChannelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EncryptedTokensJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "JobExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobExecutions_JobSettings_JobId",
                        column: x => x.JobId,
                        principalTable: "JobSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicGenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    PromptId = table.Column<int>(type: "int", nullable: false),
                    AiConnectionId = table.Column<int>(type: "int", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedCount = table.Column<int>(type: "int", nullable: false),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RenderStyle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicGenerationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicGenerationProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "ScriptGenerationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    TopicGenerationProfileId = table.Column<int>(type: "int", nullable: true),
                    PromptId = table.Column<int>(type: "int", nullable: false),
                    AiConnectionId = table.Column<int>(type: "int", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false, defaultValue: 0.80000000000000004),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValue: "en"),
                    TopicIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ProductionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RenderStyle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptGenerationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_TopicGenerationProfiles_TopicGenerationProfileId",
                        column: x => x.TopicGenerationProfileId,
                        principalTable: "TopicGenerationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScriptGenerationProfiles_UserAiConnections_AiConnectionId",
                        column: x => x.AiConnectionId,
                        principalTable: "UserAiConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Scripts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MetaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scripts_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TopicCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SubCategory = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Premise = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PremiseTr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Setting = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PotentialVisual = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NeedsFootage = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FactCheck = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopicJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptGenerated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MediaRendered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Published = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ProductionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RenderStyle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PromptId = table.Column<int>(type: "int", nullable: true),
                    ScriptId = table.Column<int>(type: "int", nullable: true),
                    GenerationAttempt = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ScriptGeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RenderedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Topics_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Topics_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_CreatedAt",
                table: "JobExecutions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobId",
                table: "JobExecutions",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobId_Status",
                table: "JobExecutions",
                columns: new[] { "JobId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_Removed",
                table: "JobExecutions",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_Status",
                table: "JobExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_AppUserId",
                table: "JobSettings",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_CreatedAt",
                table: "JobSettings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_JobType",
                table: "JobSettings",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_JobType_Status",
                table: "JobSettings",
                columns: new[] { "JobType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_Removed",
                table: "JobSettings",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_Status",
                table: "JobSettings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Category",
                table: "Prompts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_CreatedAt",
                table: "Prompts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_IsActive",
                table: "Prompts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Name",
                table: "Prompts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Removed",
                table: "Prompts",
                column: "Removed");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_AiConnectionId",
                table: "ScriptGenerationProfiles",
                column: "AiConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_AppUserId",
                table: "ScriptGenerationProfiles",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_PromptId",
                table: "ScriptGenerationProfiles",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptGenerationProfiles_TopicGenerationProfileId",
                table: "ScriptGenerationProfiles",
                column: "TopicGenerationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_TopicId",
                table: "Scripts",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_UserId",
                table: "Scripts",
                column: "UserId");

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
                name: "IX_TopicGenerationProfiles_StartedAt",
                table: "TopicGenerationProfiles",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TopicGenerationProfiles_Status",
                table: "TopicGenerationProfiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_CreatedAt",
                table: "Topics",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_PromptId",
                table: "Topics",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Removed",
                table: "Topics",
                column: "Removed");

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
                name: "IX_UserAiConnections_UserId_Name",
                table: "UserAiConnections",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSocialChannels_AppUserId_ChannelType",
                table: "UserSocialChannels",
                columns: new[] { "AppUserId", "ChannelType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Scripts_Topics_TopicId",
                table: "Scripts",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_AppUsers_UserId",
                table: "Scripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_Prompts_PromptId",
                table: "Topics");

            migrationBuilder.DropForeignKey(
                name: "FK_Scripts_Topics_TopicId",
                table: "Scripts");

            migrationBuilder.DropTable(
                name: "JobExecutions");

            migrationBuilder.DropTable(
                name: "ScriptGenerationProfiles");

            migrationBuilder.DropTable(
                name: "UserSocialChannels");

            migrationBuilder.DropTable(
                name: "JobSettings");

            migrationBuilder.DropTable(
                name: "TopicGenerationProfiles");

            migrationBuilder.DropTable(
                name: "UserAiConnections");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "Scripts");
        }
    }
}
