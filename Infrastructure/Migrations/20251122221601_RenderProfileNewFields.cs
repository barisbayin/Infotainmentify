using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenderProfileNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CaptionStyle",
                table: "AutoVideoRenderProfiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CaptionBackgroundOpacity",
                table: "AutoVideoRenderProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "CaptionChunkSize",
                table: "AutoVideoRenderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CaptionGlowColor",
                table: "AutoVideoRenderProfiles",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CaptionGlowSize",
                table: "AutoVideoRenderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CaptionHighlightColor",
                table: "AutoVideoRenderProfiles",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "CaptionLineSpacing",
                table: "AutoVideoRenderProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "CaptionMarginV",
                table: "AutoVideoRenderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CaptionMaxWidthPercent",
                table: "AutoVideoRenderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CaptionOutlineSize",
                table: "AutoVideoRenderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CaptionPosition",
                table: "AutoVideoRenderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CaptionShadowSize",
                table: "AutoVideoRenderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "MotionIntensity",
                table: "AutoVideoRenderProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "TransitionDirection",
                table: "AutoVideoRenderProfiles",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransitionEasing",
                table: "AutoVideoRenderProfiles",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "TransitionStrength",
                table: "AutoVideoRenderProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaptionBackgroundOpacity",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionChunkSize",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionGlowColor",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionGlowSize",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionHighlightColor",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionLineSpacing",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionMarginV",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionMaxWidthPercent",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionOutlineSize",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionPosition",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "CaptionShadowSize",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "MotionIntensity",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "TransitionDirection",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "TransitionEasing",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.DropColumn(
                name: "TransitionStrength",
                table: "AutoVideoRenderProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "CaptionStyle",
                table: "AutoVideoRenderProfiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);
        }
    }
}
