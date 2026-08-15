using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDiscountCampaignEnabledWithCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_discount_campaigns_enabled_start_date_end_date",
                table: "discount_campaigns");

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "discount_campaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE discount_campaigns
                SET cancelled_at = COALESCE(updated_at, created_at)
                WHERE enabled = FALSE;
                """);

            migrationBuilder.DropColumn(
                name: "enabled",
                table: "discount_campaigns");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaigns_cancelled_at_start_date_end_date",
                table: "discount_campaigns",
                columns: new[] { "cancelled_at", "start_date", "end_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_discount_campaigns_cancelled_at_start_date_end_date",
                table: "discount_campaigns");

            migrationBuilder.AddColumn<bool>(
                name: "enabled",
                table: "discount_campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE discount_campaigns SET enabled = cancelled_at IS NULL;");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "discount_campaigns");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaigns_enabled_start_date_end_date",
                table: "discount_campaigns",
                columns: new[] { "enabled", "start_date", "end_date" });
        }
    }
}
