using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStatusTypeOfLicenseReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrgUserId_IsActive",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OrganizationUserLicenseReadModels",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrgUserId_Status",
                table: "OrganizationUserLicenseReadModels",
                columns: new[] { "OrganizationUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrgUserId_Status",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrganizationUserLicenseReadModels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrgUserId_IsActive",
                table: "OrganizationUserLicenseReadModels",
                columns: new[] { "OrganizationUserId", "IsActive" });
        }
    }
}
