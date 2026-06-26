using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Basket.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddXminConcurrencyToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // xmin is a PostgreSQL system column present on every row — no DDL required.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
