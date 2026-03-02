using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSite.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarMuralAoBanco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataPostagem",
                table: "Mural",
                newName: "Data");

            migrationBuilder.RenameColumn(
                name: "CorHex",
                table: "Mural",
                newName: "CorNeon");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Data",
                table: "Mural",
                newName: "DataPostagem");

            migrationBuilder.RenameColumn(
                name: "CorNeon",
                table: "Mural",
                newName: "CorHex");
        }
    }
}
