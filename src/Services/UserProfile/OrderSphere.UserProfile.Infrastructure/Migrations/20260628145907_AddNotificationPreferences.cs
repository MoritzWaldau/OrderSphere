using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.UserProfile.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddNotificationPreferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "notification_consented_at",
            table: "CustomerProfiles",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "notification_email_enabled",
            table: "CustomerProfiles",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "notification_push_enabled",
            table: "CustomerProfiles",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "notification_sms_enabled",
            table: "CustomerProfiles",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "notification_consented_at",
            table: "CustomerProfiles");

        migrationBuilder.DropColumn(
            name: "notification_email_enabled",
            table: "CustomerProfiles");

        migrationBuilder.DropColumn(
            name: "notification_push_enabled",
            table: "CustomerProfiles");

        migrationBuilder.DropColumn(
            name: "notification_sms_enabled",
            table: "CustomerProfiles");
    }
}
