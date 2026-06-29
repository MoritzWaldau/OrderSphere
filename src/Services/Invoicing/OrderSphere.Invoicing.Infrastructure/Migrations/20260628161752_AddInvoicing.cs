using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Invoicing.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddInvoicing : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                BlobPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                items = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_InvoiceNumber",
            table: "Invoices",
            column: "InvoiceNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_OrderId",
            table: "Invoices",
            column: "OrderId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Invoices");
    }
}
