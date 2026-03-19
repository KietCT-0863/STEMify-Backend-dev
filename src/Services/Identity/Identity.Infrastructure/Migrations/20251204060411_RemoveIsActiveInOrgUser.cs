using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsActiveInOrgUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_GroupId_IsActive",
                table: "OrganizationUsers");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_Organization_Active",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrganizationUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrganizationUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Whether the user is active in the organization");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_GroupId_IsActive",
                table: "OrganizationUsers",
                columns: new[] { "GroupId", "IsActive" },
                filter: "\"GroupId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_Organization_Active",
                table: "OrganizationUsers",
                columns: new[] { "OrganizationId", "IsActive" });
        }
    }
}
