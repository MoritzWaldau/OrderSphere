using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageBlobName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageBlobName",
                table: "products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageBlobName",
                table: "products");
        }
    }
}
