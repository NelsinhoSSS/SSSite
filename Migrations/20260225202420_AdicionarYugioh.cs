using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSite.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarYugioh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YugiohDecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YugiohDecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YugiohCartas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Quantidade = table.Column<string>(type: "TEXT", nullable: false),
                    YugiohDeckId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YugiohCartas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YugiohCartas_YugiohDecks_YugiohDeckId",
                        column: x => x.YugiohDeckId,
                        principalTable: "YugiohDecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YugiohCartas_YugiohDeckId",
                table: "YugiohCartas",
                column: "YugiohDeckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YugiohCartas");

            migrationBuilder.DropTable(
                name: "YugiohDecks");
        }
    }
}
