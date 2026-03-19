using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resource.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_OrderIndex_CurriculumCourseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Courses");

            migrationBuilder.AddColumn<int>(
                name: "CourseOrderIndex",
                table: "CurriculumCourses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseOrderIndex",
                table: "CurriculumCourses");

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
