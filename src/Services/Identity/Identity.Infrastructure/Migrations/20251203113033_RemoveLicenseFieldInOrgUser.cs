using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLicenseFieldInOrgUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_LicenseAssignmentId",
                table: "OrganizationUsers");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_Org_User_Subscription",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "LicenseAssignmentId",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "SubscriptionOrderId",
                table: "OrganizationUsers");

            migrationBuilder.CreateTable(
                name: "OrganizationUserLicenseReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HasActiveStudentLicense = table.Column<bool>(type: "boolean", nullable: false),
                    HasActiveTeacherLicense = table.Column<bool>(type: "boolean", nullable: false),
                    HasActiveAdminLicense = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveStudentSubscriptionOrderId = table.Column<int>(type: "integer", nullable: true),
                    ActiveTeacherSubscriptionOrderId = table.Column<int>(type: "integer", nullable: true),
                    ActiveAdminSubscriptionOrderId = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUserLicenseReadModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_Org_User",
                table: "OrganizationUsers",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_Org_User",
                table: "OrganizationUsers");

            migrationBuilder.AddColumn<string>(
                name: "LicenseAssignmentId",
                table: "OrganizationUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "License assignment ID from Order Service");

            migrationBuilder.AddColumn<string>(
                name: "LicenseType",
                table: "OrganizationUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                comment: "License type assigned to user");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionOrderId",
                table: "OrganizationUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Subscription order ID from Order Service - REQUIRED for multi-subscription support");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_LicenseAssignmentId",
                table: "OrganizationUsers",
                column: "LicenseAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_Org_User_Subscription",
                table: "OrganizationUsers",
                columns: new[] { "OrganizationId", "UserId", "SubscriptionOrderId" },
                unique: true);
        }
    }
}
