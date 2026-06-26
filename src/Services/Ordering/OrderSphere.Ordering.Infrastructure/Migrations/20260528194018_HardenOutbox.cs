using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Ordering.Infrastructure.Migrations;

/// <inheritdoc />
public partial class HardenOutbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_outbox_messages_ProcessedAt",
            table: "outbox_messages");

        // xmin is a PostgreSQL system column present on every row — no DDL required.

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_ProcessedAt_OccurredAt",
            table: "outbox_messages",
            columns: new[] { "ProcessedAt", "OccurredAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_outbox_messages_ProcessedAt_OccurredAt",
            table: "outbox_messages");

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_ProcessedAt",
            table: "outbox_messages",
            column: "ProcessedAt");
    }
}
