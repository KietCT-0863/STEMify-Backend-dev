using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCourseEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Certificates_CurriculumEnrollmentId",
                table: "Certificates");

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumEnrollmentId",
                table: "CourseEnrollments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CurriculumEnrollmentId",
                table: "Certificates",
                column: "CurriculumEnrollmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Certificates_CurriculumEnrollmentId",
                table: "Certificates");

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumEnrollmentId",
                table: "CourseEnrollments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CurriculumEnrollmentId",
                table: "Certificates",
                column: "CurriculumEnrollmentId");
        }
    }
}
