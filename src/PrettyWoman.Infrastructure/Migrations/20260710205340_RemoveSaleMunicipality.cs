using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSaleMunicipality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_municipalities_municipality_id",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_municipality_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "municipality_id",
                table: "sales");

            migrationBuilder.AddColumn<bool>(
                name: "can_collect_cash_on_delivery",
                table: "delivery_agencies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_collect_cash_on_delivery",
                table: "delivery_agencies");

            migrationBuilder.AddColumn<int>(
                name: "municipality_id",
                table: "sales",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_municipality_id",
                table: "sales",
                column: "municipality_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_municipalities_municipality_id",
                table: "sales",
                column: "municipality_id",
                principalTable: "municipalities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
