using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraHub.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoNameToPricingPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "promo_name",
                table: "pricing_policies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Backfill legacy promos to ensure production-safe, non-empty promo names where promo pricing exists.
            migrationBuilder.Sql("""
                UPDATE pricing_policies
                SET promo_name = 'Book promo'
                WHERE promo_price IS NOT NULL
                  AND (promo_name IS NULL OR btrim(promo_name) = '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "promo_name",
                table: "pricing_policies");
        }
    }
}
