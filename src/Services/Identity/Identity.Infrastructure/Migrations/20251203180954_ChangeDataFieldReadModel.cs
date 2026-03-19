using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDataFieldReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganizationUserLicenseReadModels",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "ActiveAdminSubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "ActiveStudentSubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "ActiveTeacherSubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "HasActiveAdminLicense",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "HasActiveStudentLicense",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.RenameColumn(
                name: "HasActiveTeacherLicense",
                table: "OrganizationUserLicenseReadModels",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "OrganizationUserLicenseReadModels",
                newName: "OrganizationUserId");

            migrationBuilder.AddColumn<int>(
                name: "LicenseAssignmentId",
                table: "OrganizationUserLicenseReadModels",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "OrganizationUserLicenseReadModels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LicenseType",
                table: "OrganizationUserLicenseReadModels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrganizationUserLicenseReadModels",
                table: "OrganizationUserLicenseReadModels",
                column: "LicenseAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrganizationId",
                table: "OrganizationUserLicenseReadModels",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrganizationUserId",
                table: "OrganizationUserLicenseReadModels",
                column: "OrganizationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrgUserId_IsActive",
                table: "OrganizationUserLicenseReadModels",
                columns: new[] { "OrganizationUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserLicenseReadModels_SubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels",
                column: "SubscriptionOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganizationUserLicenseReadModels",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrganizationId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrganizationUserId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserLicenseReadModels_OrgUserId_IsActive",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserLicenseReadModels_SubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "LicenseAssignmentId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.DropColumn(
                name: "SubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels");

            migrationBuilder.RenameColumn(
                name: "OrganizationUserId",
                table: "OrganizationUserLicenseReadModels",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "OrganizationUserLicenseReadModels",
                newName: "HasActiveTeacherLicense");

            migrationBuilder.AddColumn<int>(
                name: "ActiveAdminSubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActiveStudentSubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActiveTeacherSubscriptionOrderId",
                table: "OrganizationUserLicenseReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasActiveAdminLicense",
                table: "OrganizationUserLicenseReadModels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasActiveStudentLicense",
                table: "OrganizationUserLicenseReadModels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrganizationUserLicenseReadModels",
                table: "OrganizationUserLicenseReadModels",
                column: "Id");
        }
    }
}
