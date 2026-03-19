using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClassroomAndEnrollmentFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CurriculumId",
                table: "Classroom",
                newName: "CourseId");

            migrationBuilder.AddColumn<int>(
                name: "ClassroomId",
                table: "CourseEnrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_ClassroomId",
                table: "CourseEnrollments",
                column: "ClassroomId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Classroom_ClassroomId",
                table: "CourseEnrollments",
                column: "ClassroomId",
                principalTable: "Classroom",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Classroom_ClassroomId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_ClassroomId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                table: "CourseEnrollments");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Classroom",
                newName: "CurriculumId");
        }
    }
}
