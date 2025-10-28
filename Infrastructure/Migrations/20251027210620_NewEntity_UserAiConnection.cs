using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewEntity_UserAiConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAiConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    AuthType = table.Column<int>(type: "int", nullable: false),
                    Capabilities = table.Column<int>(type: "int", nullable: false),
                    IsDefaultForText = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultForImage = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultForEmbedding = table.Column<bool>(type: "bit", nullable: false),
                    EncryptedCredentialJson = table.Column<string>(type: "nvarchar(max)", maxLength: 4000, nullable: false),
                    AccessTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    ChannelType = table.Column<int>(type: "int", nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ChannelName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChannelHandle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChannelUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PlatformChannelId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EncryptedTokensJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAiConnections");

            migrationBuilder.DropTable(
                name: "UserSocialChannels");
        }
    }
}
