using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraHub.Notifications.Infrastructure.Migrations
{
    public partial class AddUserNotificationSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_notification_settings",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_staff = table.Column<bool>(type: "boolean", nullable: false),
                    email_announcements_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    email_promotions_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notification_settings", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_settings_email_announcements_enabled",
                table: "user_notification_settings",
                column: "email_announcements_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_settings_email_promotions_enabled",
                table: "user_notification_settings",
                column: "email_promotions_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_settings_is_staff",
                table: "user_notification_settings",
                column: "is_staff");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_notification_settings");
        }
    }
}
