using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraHub.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Net10ModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_orders_user_id_status",
                table: "orders",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_user_id_status",
                table: "orders");
        }
    }
}
