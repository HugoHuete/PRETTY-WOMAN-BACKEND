using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SaleStatusesAndPaymentStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "sale_status_id",
                table: "sales",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "sale_payment_status_id",
                table: "sales",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "sale_payment_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_payment_statuses", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "sale_payment_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Unpaid" },
                    { 2, "PartiallyPaid" },
                    { 3, "Paid" }
                });

            migrationBuilder.InsertData(
                table: "sale_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 4, "SentForDelivery" },
                    { 5, "Completed_Migration" },
                    { 6, "Cancelled_Migration" }
                });

            migrationBuilder.UpdateData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 2,
                column: "name",
                value: "Reserved");

            migrationBuilder.UpdateData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 3,
                column: "name",
                value: "ReadyForDelivery");

            migrationBuilder.UpdateData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 5,
                column: "name",
                value: "Completed");

            migrationBuilder.UpdateData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 6,
                column: "name",
                value: "Cancelled");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_payment_status_id_created_at",
                table: "sales",
                columns: new[] { "sale_payment_status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_statuses_name",
                table: "sale_payment_statuses",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_sale_payment_statuses_sale_payment_status_id",
                table: "sales",
                column: "sale_payment_status_id",
                principalTable: "sale_payment_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_sale_payment_statuses_sale_payment_status_id",
                table: "sales");

            migrationBuilder.DropTable(
                name: "sale_payment_statuses");

            migrationBuilder.DropIndex(
                name: "ix_sales_sale_payment_status_id_created_at",
                table: "sales");

            migrationBuilder.DeleteData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "sale_payment_status_id",
                table: "sales");

            migrationBuilder.AlterColumn<int>(
                name: "sale_status_id",
                table: "sales",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.UpdateData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 2,
                column: "name",
                value: "Completed");

            migrationBuilder.UpdateData(
                table: "sale_statuses",
                keyColumn: "id",
                keyValue: 3,
                column: "name",
                value: "Cancelled");
        }
    }
}
