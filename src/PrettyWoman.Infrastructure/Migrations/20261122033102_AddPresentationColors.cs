using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "color_id",
                table: "product_presentations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "colors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hex_code = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colors", x => x.id);
                    table.CheckConstraint("ck_colors_hex_code", "hex_code ~ '^#[0-9A-Fa-f]{6}$'");
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_presentations_color_id",
                table: "product_presentations",
                column: "color_id");

            migrationBuilder.CreateIndex(
                name: "ix_colors_name",
                table: "colors",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_presentations_colors_color_id",
                table: "product_presentations",
                column: "color_id",
                principalTable: "colors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
            migrationBuilder.Sql("""
                INSERT INTO colors (name, hex_code) VALUES
                    ('Blanco', '#FFFFFF'),
                    ('Negro', '#171717'),
                    ('Marfil', '#FFFFF0'),
                    ('Ivory', '#FFFFF0'),
                    ('Blanco Hueso', '#F2E9D8'),
                    ('Champán', '#E8D5AB'),
                    ('Crema', '#FFF3D6'),
                    ('Beige', '#D6BD99'),
                    ('Arena', '#C7A875'),
                    ('Camel', '#B88756'),
                    ('Café', '#684530'),
                    ('Café integral', '#80604C'),
                    ('Marrón', '#784F36'),
                    ('Chocolate', '#482C22'),
                    ('Gris', '#8C8C8C'),
                    ('Gris carbón', '#42464B'),
                    ('Plata', '#C0C0C0'),
                    ('Plateado', '#C0C0C0'),
                    ('Dorado', '#C5A04B'),
                    ('Rojo', '#C51E32'),
                    ('Rojo óxido oscuro', '#7B3025'),
                    ('Rojo Vino', '#6C273D'),
                    ('Rojo violeta', '#A52A67'),
                    ('Rojo Oscuro', '#8B1A24'),
                    ('Burdeos', '#722F37'),
                    ('Burgundy', '#722F37'),
                    ('Granate', '#800020'),
                    ('Vino', '#6C273D'),
                    ('Rosado', '#E58AAE'),
                    ('Rosa', '#E58AAE'),
                    ('Rosa palo', '#C98991'),
                    ('Fucsia', '#D52982'),
                    ('Coral', '#F07769'),
                    ('Albaricoque', '#F5BA8A'),
                    ('Naranja', '#EA792E'),
                    ('Terracota', '#B85A41'),
                    ('Amarillo', '#F4CB49'),
                    ('Amarillo claro', '#F9E7A0'),
                    ('Mostaza', '#C89C27'),
                    ('Verde', '#39804E'),
                    ('Verde Oscuro', '#244D35'),
                    ('Verde oliva', '#737750'),
                    ('Verde militar', '#545A40'),
                    ('Verde menta', '#A8D9C4'),
                    ('Verde esmeralda', '#00845B'),
                    ('Azul', '#2C5DAA'),
                    ('Azul Marino', '#1F2B49'),
                    ('Azul Royal', '#3054BF'),
                    ('Azul Denim', '#486F94'),
                    ('Celeste', '#9CCBEA'),
                    ('Turquesa', '#36AAAB'),
                    ('Morado', '#71458E'),
                    ('Lila', '#B49AD0'),
                    ('Lavanda', '#CABAE4');

                UPDATE product_presentations AS presentation
                SET color_id = color.id
                FROM colors AS color
                WHERE presentation.color_id IS NULL
                  AND (
                    lower(translate(btrim(presentation.name), 'áéíóúüñÁÉÍÓÚÜÑ', 'aeiouunAEIOUUN')) =
                        lower(translate(color.name, 'áéíóúüñÁÉÍÓÚÜÑ', 'aeiouunAEIOUUN'))
                    OR (lower(btrim(presentation.name)) = 'beis' AND color.name = 'Beige')
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_presentations_colors_color_id",
                table: "product_presentations");

            migrationBuilder.DropTable(
                name: "colors");

            migrationBuilder.DropIndex(
                name: "ix_product_presentations_color_id",
                table: "product_presentations");

            migrationBuilder.DropColumn(
                name: "color_id",
                table: "product_presentations");
        }
    }
}
