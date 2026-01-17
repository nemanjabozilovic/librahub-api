using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraHub.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns
            migrationBuilder.AddColumn<bool>(
                name: "email_enabled",
                table: "user_notification_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "in_app_enabled",
                table: "user_notification_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Migrate data: if either email_announcements_enabled or email_promotions_enabled was true, set email_enabled to true
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                        AND table_name = 'user_notification_settings'
                        AND (column_name = 'email_announcements_enabled' OR column_name = 'email_promotions_enabled')
                    ) THEN
                        UPDATE user_notification_settings
                        SET email_enabled = (COALESCE(email_announcements_enabled, false) OR COALESCE(email_promotions_enabled, false)),
                            in_app_enabled = true
                        WHERE email_enabled = false;
                    END IF;
                END $$;
            ");

            // Migrate preferences from notification_preferences table
            // If user has any notification preference with email enabled, enable email
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public'
                        AND table_name = 'notification_preferences'
                    ) THEN
                        UPDATE user_notification_settings u
                        SET email_enabled = true
                        FROM notification_preferences np
                        WHERE u.user_id = np.user_id
                          AND np.email_enabled = true
                          AND u.email_enabled = false;

                        UPDATE user_notification_settings u
                        SET in_app_enabled = true
                        FROM notification_preferences np
                        WHERE u.user_id = np.user_id
                          AND np.in_app_enabled = true
                          AND u.in_app_enabled = false;
                    END IF;
                END $$;
            ");

            // Drop old columns (if they exist)
            migrationBuilder.Sql(@"
                ALTER TABLE user_notification_settings DROP COLUMN IF EXISTS email_announcements_enabled;
                ALTER TABLE user_notification_settings DROP COLUMN IF EXISTS email_promotions_enabled;
            ");

            // Drop old indexes (if they exist)
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_user_notification_settings_email_announcements_enabled"";
                DROP INDEX IF EXISTS ""IX_user_notification_settings_email_promotions_enabled"";
            ");

            // Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_user_notification_settings_email_enabled",
                table: "user_notification_settings",
                column: "email_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_settings_in_app_enabled",
                table: "user_notification_settings",
                column: "in_app_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_settings_is_active_is_staff",
                table: "user_notification_settings",
                columns: new[] { "is_active", "is_staff" });

            // Drop notification_preferences table (if it exists)
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS notification_preferences;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_notification_settings_is_active_is_staff",
                table: "user_notification_settings");

            migrationBuilder.RenameColumn(
                name: "in_app_enabled",
                table: "user_notification_settings",
                newName: "email_promotions_enabled");

            migrationBuilder.RenameColumn(
                name: "email_enabled",
                table: "user_notification_settings",
                newName: "email_announcements_enabled");

            migrationBuilder.RenameIndex(
                name: "IX_user_notification_settings_in_app_enabled",
                table: "user_notification_settings",
                newName: "IX_user_notification_settings_email_promotions_enabled");

            migrationBuilder.RenameIndex(
                name: "IX_user_notification_settings_email_enabled",
                table: "user_notification_settings",
                newName: "IX_user_notification_settings_email_announcements_enabled");

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_id",
                table: "notification_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_id_type",
                table: "notification_preferences",
                columns: new[] { "user_id", "type" },
                unique: true);
        }
    }
}
