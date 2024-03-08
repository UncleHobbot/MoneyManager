using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAlternativeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternativeName1",
                table: "Accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternativeName2",
                table: "Accounts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternativeName1",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AlternativeName2",
                table: "Accounts");
        }
    }
}
