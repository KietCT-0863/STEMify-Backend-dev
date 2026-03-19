using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClassIdInOrgUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "OrganizationUsers");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "Invitations",
                newName: "GroupName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GroupName",
                table: "Invitations",
                newName: "ClassId");

            migrationBuilder.AddColumn<string>(
                name: "ClassId",
                table: "OrganizationUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "Class ID for students");
        }
    }
}
