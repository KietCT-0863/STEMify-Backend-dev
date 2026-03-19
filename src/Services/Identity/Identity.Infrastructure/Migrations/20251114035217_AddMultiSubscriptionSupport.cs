using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSubscriptionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_User_Organization",
                table: "OrganizationUsers");

            migrationBuilder.AlterColumn<int>(
                name: "SubscriptionOrderId",
                table: "OrganizationUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Subscription order ID from Order Service - REQUIRED for multi-subscription support",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Subscription order ID from Order Service");

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "OrganizationUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "User bio/description within this subscription");

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentDateOfBirth",
                table: "OrganizationUsers",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Student date of birth (only for Student role)");

            migrationBuilder.AddColumn<string>(
                name: "StudentMajor",
                table: "OrganizationUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Student major (only for Student role)");

            migrationBuilder.AddColumn<string>(
                name: "TeacherSpecialization",
                table: "OrganizationUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Teacher specialization (only for Teacher role)");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_Org_User_Subscription",
                table: "OrganizationUsers",
                columns: new[] { "OrganizationId", "UserId", "SubscriptionOrderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUsers_Org_User_Subscription",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "StudentDateOfBirth",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "StudentMajor",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "TeacherSpecialization",
                table: "OrganizationUsers");

            migrationBuilder.AlterColumn<int>(
                name: "SubscriptionOrderId",
                table: "OrganizationUsers",
                type: "integer",
                nullable: true,
                comment: "Subscription order ID from Order Service",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Subscription order ID from Order Service - REQUIRED for multi-subscription support");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_User_Organization",
                table: "OrganizationUsers",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true,
                filter: "\"IsActive\" = true");
        }
    }
}
