using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VAH.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailLg",
                table: "Assets",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailMd",
                table: "Assets",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailSm",
                table: "Assets",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailLg",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ThumbnailMd",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ThumbnailSm",
                table: "Assets");
        }
    }
}
