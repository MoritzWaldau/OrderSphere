using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.UserProfile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameKeycloakSubjectToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KeycloakSubject",
                table: "CustomerProfiles",
                newName: "Subject");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerProfiles_KeycloakSubject",
                table: "CustomerProfiles",
                newName: "IX_CustomerProfiles_Subject");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "CustomerProfiles",
                newName: "KeycloakSubject");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerProfiles_Subject",
                table: "CustomerProfiles",
                newName: "IX_CustomerProfiles_KeycloakSubject");
        }
    }
}
