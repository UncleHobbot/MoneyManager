using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyManager.Migrations
{
    /// <inheritdoc />
    public partial class AddBoolFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRuleApplied",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHideFromGraph",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRuleApplied",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsHideFromGraph",
                table: "Accounts");
        }
    }
}
