using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostSaleDeliveryCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount_collected_nio",
                table: "post_sale_deliveries",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_collected_usd",
                table: "post_sale_deliveries",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_to_collect",
                table: "post_sale_deliveries",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "change_given_nio",
                table: "post_sale_deliveries",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "collection_exchange_rate",
                table: "post_sale_deliveries",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amount_collected_nio",
                table: "post_sale_deliveries");

            migrationBuilder.DropColumn(
                name: "amount_collected_usd",
                table: "post_sale_deliveries");

            migrationBuilder.DropColumn(
                name: "amount_to_collect",
                table: "post_sale_deliveries");

            migrationBuilder.DropColumn(
                name: "change_given_nio",
                table: "post_sale_deliveries");

            migrationBuilder.DropColumn(
                name: "collection_exchange_rate",
                table: "post_sale_deliveries");
        }
    }
}
