using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraHub.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFormatAndVersionToReadingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reading_progress_user_id_book_id",
                table: "reading_progress");

            migrationBuilder.AddColumn<string>(
                name: "format",
                table: "reading_progress",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "reading_progress",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reading_progress_user_id_book_id_format_version",
                table: "reading_progress",
                columns: new[] { "user_id", "book_id", "format", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reading_progress_user_id_book_id_format_version",
                table: "reading_progress");

            migrationBuilder.DropColumn(
                name: "format",
                table: "reading_progress");

            migrationBuilder.DropColumn(
                name: "version",
                table: "reading_progress");

            migrationBuilder.CreateIndex(
                name: "IX_reading_progress_user_id_book_id",
                table: "reading_progress",
                columns: new[] { "user_id", "book_id" },
                unique: true);
        }
    }
}
