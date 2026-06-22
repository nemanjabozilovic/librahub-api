using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraHub.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Net10ModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_audit");

            migrationBuilder.DropTable(
                name: "promotion_rules");

            migrationBuilder.DropTable(
                name: "promotion_campaigns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_audit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ends_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    stacking_policy = table.Column<int>(type: "integer", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_campaigns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    applies_to_scope = table.Column<int>(type: "integer", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    exclusions = table.Column<string>(type: "jsonb", nullable: true),
                    max_discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    min_price_after_discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    scope_value = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_rules_promotion_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "promotion_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_audit_at_utc",
                table: "promotion_audit",
                column: "at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_audit_campaign_id",
                table: "promotion_audit",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_rules_campaign_id",
                table: "promotion_rules",
                column: "campaign_id");
        }
    }
}
