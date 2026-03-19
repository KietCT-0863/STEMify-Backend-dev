using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsAndGroupIdToOrganizationUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "OrganizationUsers",
                type: "integer",
                nullable: true,
                comment: "Group ID");

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false, comment: "Organization ID"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Group name"),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Group description"),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Group code (unique within organization)"),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Group status: Active or Archived"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "User ID who created the group"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Record creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_GroupId",
                table: "OrganizationUsers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_GroupId_IsActive",
                table: "OrganizationUsers",
                columns: new[] { "GroupId", "IsActive" },
                filter: "\"GroupId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizationId",
                table: "Groups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizationId_Code",
                table: "Groups",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizationId_Name",
                table: "Groups",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizationId_Status",
                table: "Groups",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationUsers_Groups_GroupId",
                table: "OrganizationUsers",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationUsers_Groups_GroupId",
                table: "OrganizationUsers");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_GroupId",
                table: "OrganizationUsers");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_GroupId_IsActive",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "OrganizationUsers");
        }
    }
}
