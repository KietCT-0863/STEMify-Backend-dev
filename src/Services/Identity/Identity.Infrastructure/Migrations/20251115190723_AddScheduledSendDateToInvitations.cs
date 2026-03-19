using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledSendDateToInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledSendDate",
                table: "Invitations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Date when email should be sent (for scheduled invitations). If null, email should be sent immediately");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Teacher last name");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Teacher first name");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ScheduledSendDate_Status",
                table: "Invitations",
                columns: new[] { "ScheduledSendDate", "Status" },
                filter: "\"ScheduledSendDate\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invitations_ScheduledSendDate_Status",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "ScheduledSendDate",
                table: "Invitations");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Teacher last name",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Teacher first name",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Bio = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, comment: "Student biography/description"),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Student date of birth"),
                    Major = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, comment: "Student major/field of study")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Bio = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, comment: "Teacher biography/description"),
                    Specialization = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, comment: "Teacher specialization/subject area")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_DateOfBirth",
                table: "Students",
                column: "DateOfBirth");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Major",
                table: "Students",
                column: "Major");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_Specialization",
                table: "Teachers",
                column: "Specialization");
        }
    }
}
