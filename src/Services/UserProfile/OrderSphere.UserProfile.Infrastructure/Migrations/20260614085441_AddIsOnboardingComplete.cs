using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.UserProfile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOnboardingComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnboardingComplete",
                table: "CustomerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: existing profiles that already have at least one active address
            // are considered complete so they are not blocked by the onboarding gate.
            migrationBuilder.Sql("""
                UPDATE "CustomerProfiles" cp
                SET "IsOnboardingComplete" = true
                WHERE EXISTS (
                    SELECT 1 FROM "SavedAddresses" sa
                    WHERE sa."CustomerProfileId" = cp."Id"
                      AND sa."IsDeleted" = false
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnboardingComplete",
                table: "CustomerProfiles");
        }
    }
}
