using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFK_CourseEnrollmentTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StudentLessonProgress",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                comment: "Progress status: InProgress, Completed, Failed.",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldComment: "Progress status: NotStarted, InProgress, Completed, Submitted, Failed.");

            migrationBuilder.AddColumn<int>(
                name: "CurriculumEnrollmentId",
                table: "CourseEnrollments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CurriculumEnrollmentId",
                table: "CourseEnrollments",
                column: "CurriculumEnrollmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_CurriculumEnrollments_CurriculumEnrollmen~",
                table: "CourseEnrollments",
                column: "CurriculumEnrollmentId",
                principalTable: "CurriculumEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_CurriculumEnrollments_CurriculumEnrollmen~",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CurriculumEnrollmentId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "CurriculumEnrollmentId",
                table: "CourseEnrollments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StudentLessonProgress",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                comment: "Progress status: NotStarted, InProgress, Completed, Submitted, Failed.",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldComment: "Progress status: InProgress, Completed, Failed.");
        }
    }
}
