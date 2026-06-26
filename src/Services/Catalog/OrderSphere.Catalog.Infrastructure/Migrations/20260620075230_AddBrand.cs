using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Catalog.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddBrand : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "BrandId",
            table: "products",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "brands",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                LogoBlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_brands", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_products_BrandId",
            table: "products",
            column: "BrandId");

        migrationBuilder.CreateIndex(
            name: "IX_brands_name",
            table: "brands",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_brands_slug",
            table: "brands",
            column: "Slug",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_products_brands_BrandId",
            table: "products",
            column: "BrandId",
            principalTable: "brands",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_products_brands_BrandId",
            table: "products");

        migrationBuilder.DropTable(
            name: "brands");

        migrationBuilder.DropIndex(
            name: "IX_products_BrandId",
            table: "products");

        migrationBuilder.DropColumn(
            name: "BrandId",
            table: "products");
    }
}
