using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgId_ClassroomTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Classroom",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumEnrollments_ClassroomId",
                table: "CurriculumEnrollments",
                column: "ClassroomId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumEnrollments_Classroom_ClassroomId",
                table: "CurriculumEnrollments",
                column: "ClassroomId",
                principalTable: "Classroom",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumEnrollments_Classroom_ClassroomId",
                table: "CurriculumEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumEnrollments_ClassroomId",
                table: "CurriculumEnrollments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Classroom");
        }
    }
}
