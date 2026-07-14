using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "product_images");

            migrationBuilder.AddColumn<Guid>(
                name: "media_asset_id",
                table: "product_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_bucket = table.Column<int>(type: "integer", nullable: false),
                    visibility = table.Column<int>(type: "integer", nullable: false),
                    original_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    original_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "media_asset_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    bucket = table.Column<int>(type: "integer", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_asset_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_asset_variants_media_assets_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_media_asset_id",
                table: "product_images",
                column: "media_asset_id",
                unique: true,
                filter: "media_asset_id is not null");

            migrationBuilder.CreateIndex(
                name: "ix_media_asset_variants_bucket_storage_key",
                table: "media_asset_variants",
                columns: new[] { "bucket", "storage_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_asset_variants_media_asset_id_type",
                table: "media_asset_variants",
                columns: new[] { "media_asset_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_storage_key",
                table: "media_assets",
                column: "storage_key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_images_media_assets_media_asset_id",
                table: "product_images",
                column: "media_asset_id",
                principalTable: "media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_images_media_assets_media_asset_id",
                table: "product_images");

            migrationBuilder.DropTable(
                name: "media_asset_variants");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropIndex(
                name: "ix_product_images_media_asset_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "media_asset_id",
                table: "product_images");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "product_images",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }
    }
}
