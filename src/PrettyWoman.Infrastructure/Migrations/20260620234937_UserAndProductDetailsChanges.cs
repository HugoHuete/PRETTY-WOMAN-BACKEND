using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserAndProductDetailsChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "lasname",
                table: "AspNetUsers",
                newName: "lastname");

            migrationBuilder.Sql("""
                ALTER TABLE product_details
                ALTER COLUMN code TYPE integer
                USING code::integer;
            """);

            migrationBuilder.CreateIndex(
                name: "ix_product_details_code",
                table: "product_details",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_details_code",
                table: "product_details");

            migrationBuilder.RenameColumn(
                name: "lastname",
                table: "AspNetUsers",
                newName: "lasname");

            migrationBuilder.Sql("""
                ALTER TABLE product_details
                ALTER COLUMN code TYPE character varying(10)
                USING code::varchar(10);
            """);
        }
    }
}
