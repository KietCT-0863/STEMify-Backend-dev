using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Resource.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePK_CurriculumCourseTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CurriculumCourses",
                table: "CurriculumCourses");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CurriculumCourses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CurriculumCourses",
                table: "CurriculumCourses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumCourses_CourseId",
                table: "CurriculumCourses",
                column: "CourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CurriculumCourses",
                table: "CurriculumCourses");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumCourses_CourseId",
                table: "CurriculumCourses");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CurriculumCourses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CurriculumCourses",
                table: "CurriculumCourses",
                columns: new[] { "CourseId", "CurriculumId" });
        }
    }
}
