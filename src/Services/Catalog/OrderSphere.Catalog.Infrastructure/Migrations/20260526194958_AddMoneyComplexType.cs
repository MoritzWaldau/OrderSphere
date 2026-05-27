using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoneyComplexType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "products",
                newName: "price");

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                table: "products",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "price_currency",
                table: "products");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "products",
                newName: "Price");
        }
    }
}
