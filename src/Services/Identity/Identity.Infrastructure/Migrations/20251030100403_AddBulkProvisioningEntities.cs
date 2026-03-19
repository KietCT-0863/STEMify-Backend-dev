using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkProvisioningEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false, comment: "Organization ID from Order Service"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Job processing status"),
                    TotalCount = table.Column<int>(type: "integer", nullable: false, comment: "Total number of users to invite"),
                    ProcessedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of users processed"),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of successful invitations"),
                    FailedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of failed invitations"),
                    CsvDataJson = table.Column<string>(type: "jsonb", nullable: false, comment: "Serialized CSV data for processing"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false, comment: "User ID who created the job"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Job processing start timestamp"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Job completion timestamp"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Job creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false, comment: "Organization ID from Order Service"),
                    InviteeEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Email address of the invitee"),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Unique invitation token"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Invitation status"),
                    TargetRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "Role to assign to user"),
                    LicenseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "License type to assign"),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Full name from CSV"),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "First name from CSV"),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Last name from CSV"),
                    ClassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Class ID from CSV"),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "External ID from CSV"),
                    SubscriptionOrderId = table.Column<int>(type: "integer", nullable: true, comment: "Order ID of the subscription"),
                    InvitedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Invitation expiration date"),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Email sent timestamp"),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Invitation accepted timestamp"),
                    AcceptedUserId = table.Column<Guid>(type: "uuid", nullable: true, comment: "User ID who accepted the invitation"),
                    ProcessedByJobId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Bulk import job ID that created this invitation"),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Invitation creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false, comment: "Organization ID from Order Service - no FK constraint"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "User ID from Identity Service"),
                    OrganizationRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "User role within the organization"),
                    LicenseAssignmentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "License assignment ID from Order Service"),
                    LicenseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "License type assigned to user"),
                    SubscriptionOrderId = table.Column<int>(type: "integer", nullable: true, comment: "Subscription order ID from Order Service"),
                    ClassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Class ID for students"),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when user joined organization"),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when user left organization"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Whether the user is active in the organization"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Record creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BulkImportFailures",
                columns: table => new
                {
                    BulkImportJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportFailures", x => new { x.BulkImportJobId, x.Id });
                    table.ForeignKey(
                        name: "FK_BulkImportFailures_BulkImportJobs_BulkImportJobId",
                        column: x => x.BulkImportJobId,
                        principalTable: "BulkImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportJobs_CreatedBy",
                table: "BulkImportJobs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportJobs_Organization_Created",
                table: "BulkImportJobs",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportJobs_OrganizationId",
                table: "BulkImportJobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportJobs_Status",
                table: "BulkImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_AcceptedUserId",
                table: "Invitations",
                column: "AcceptedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_JobId",
                table: "Invitations",
                column: "ProcessedByJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Organization_Status",
                table: "Invitations",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Status_Expiry",
                table: "Invitations",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_LicenseAssignmentId",
                table: "OrganizationUsers",
                column: "LicenseAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_Organization_Active",
                table: "OrganizationUsers",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_Organization_Role",
                table: "OrganizationUsers",
                columns: new[] { "OrganizationId", "OrganizationRole" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_OrganizationId",
                table: "OrganizationUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_User_Organization",
                table: "OrganizationUsers",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_UserId",
                table: "OrganizationUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkImportFailures");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "OrganizationUsers");

            migrationBuilder.DropTable(
                name: "BulkImportJobs");
        }
    }
}
