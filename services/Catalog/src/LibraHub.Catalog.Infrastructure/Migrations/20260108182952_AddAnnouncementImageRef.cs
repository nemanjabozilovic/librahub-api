using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraHub.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementImageRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_ref",
                table: "announcements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_ref",
                table: "announcements");
        }
    }
}
