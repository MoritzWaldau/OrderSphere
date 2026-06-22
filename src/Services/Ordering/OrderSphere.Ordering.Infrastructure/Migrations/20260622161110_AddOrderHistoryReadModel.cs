using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderHistoryReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_history_CustomerEmail",
                table: "order_history",
                column: "CustomerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_order_history_OccurredAt",
                table: "order_history",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_order_history_OrderId_OccurredAt",
                table: "order_history",
                columns: new[] { "OrderId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_history");
        }
    }
}
