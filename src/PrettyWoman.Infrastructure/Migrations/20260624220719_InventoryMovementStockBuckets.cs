using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryMovementStockBuckets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_stock_buckets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_stock_buckets", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "inventory_stock_buckets",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "External" },
                    { 2, "Available" },
                    { 3, "Reserved" },
                    { 4, "Unavailable" },
                    { 5, "OutOfInventory" }
                });

            migrationBuilder.AddColumn<int>(
                name: "from_stock_bucket_id",
                table: "inventory_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "to_stock_bucket_id",
                table: "inventory_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "from_stock_bucket_id",
                table: "inventory_movements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "to_stock_bucket_id",
                table: "inventory_movements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_movement_directions_movement_direction_",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_movement_direction_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "movement_direction_id",
                table: "inventory_movements");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_from_stock_bucket_id",
                table: "inventory_movements",
                column: "from_stock_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_to_stock_bucket_id",
                table: "inventory_movements",
                column: "to_stock_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_stock_buckets_name",
                table: "inventory_stock_buckets",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_inventory_stock_buckets_from_stock_buck",
                table: "inventory_movements",
                column: "from_stock_bucket_id",
                principalTable: "inventory_stock_buckets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_inventory_stock_buckets_to_stock_bucket",
                table: "inventory_movements",
                column: "to_stock_bucket_id",
                principalTable: "inventory_stock_buckets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_inventory_stock_buckets_from_stock_buck",
                table: "inventory_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_inventory_stock_buckets_to_stock_bucket",
                table: "inventory_movements");

            migrationBuilder.DropTable(
                name: "inventory_stock_buckets");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_from_stock_bucket_id",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_to_stock_bucket_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "from_stock_bucket_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "to_stock_bucket_id",
                table: "inventory_movements");

            migrationBuilder.AddColumn<int>(
                name: "movement_direction_id",
                table: "inventory_movements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_movement_direction_id",
                table: "inventory_movements",
                column: "movement_direction_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_movement_directions_movement_direction_",
                table: "inventory_movements",
                column: "movement_direction_id",
                principalTable: "movement_directions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
