using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderSphere.Ordering.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCoupons : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "coupons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                DiscountType = table.Column<int>(type: "integer", nullable: false),
                Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                MinSubtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                RedeemedCount = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_coupons", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "coupons",
            columns: new[] { "Id", "Code", "CreatedAt", "DiscountType", "IsActive", "IsDeleted", "MaxRedemptions", "MinSubtotal", "RedeemedCount", "UpdatedAt", "ValidFrom", "ValidUntil", "Value" },
            values: new object[,]
            {
                { new Guid("0192a000-0000-7000-8000-000000000001"), "WELCOME10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, true, false, null, null, 0, null, null, null, 10m },
                { new Guid("0192a000-0000-7000-8000-000000000002"), "SUMMER15", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, true, false, null, 100m, 0, null, null, null, 15m }
            });

        migrationBuilder.CreateIndex(
            name: "IX_coupons_Code",
            table: "coupons",
            column: "Code",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "coupons");
    }
}
