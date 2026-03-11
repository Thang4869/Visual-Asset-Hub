using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VAH.Backend.Migrations
{
    /// <summary>
    /// One-time data fix: assets created before the factory fix had ContentType='file'
    /// for all subtypes. Re-classify based on collection type and asset characteristics.
    /// </summary>
    public partial class FixContentTypeDiscriminator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Assets SET ContentType = 'image'
                WHERE ContentType = 'file' AND FilePath LIKE '/uploads/%'
                  AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'image');
            ");

            migrationBuilder.Sql(@"
                UPDATE Assets SET ContentType = 'link'
                WHERE ContentType = 'file' AND FilePath LIKE 'http%'
                  AND IsFolder = 0;
            ");

            migrationBuilder.Sql(@"
                UPDATE Assets SET ContentType = 'color'
                WHERE ContentType = 'file' AND FilePath LIKE '#%'
                  AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'color');
            ");

            migrationBuilder.Sql(@"
                UPDATE Assets SET ContentType = 'color-group'
                WHERE ContentType = 'file' AND FilePath = ''
                  AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'color');
            ");

            migrationBuilder.Sql(@"
                UPDATE Assets SET ContentType = 'folder'
                WHERE ContentType = 'file' AND IsFolder = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no rollback — the original 'file' discriminator was incorrect data.
        }
    }
}
