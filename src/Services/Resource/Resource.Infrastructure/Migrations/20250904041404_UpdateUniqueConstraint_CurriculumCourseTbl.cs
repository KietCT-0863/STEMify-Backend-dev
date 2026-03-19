using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resource.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUniqueConstraint_CurriculumCourseTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurriculumCourses_CourseId",
                table: "CurriculumCourses");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumCourses_CourseId_CurriculumId",
                table: "CurriculumCourses",
                columns: new[] { "CourseId", "CurriculumId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurriculumCourses_CourseId_CurriculumId",
                table: "CurriculumCourses");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumCourses_CourseId",
                table: "CurriculumCourses",
                column: "CourseId");
        }
    }
}
