using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomStudentTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassroomResource");

            migrationBuilder.AddColumn<int>(
                name: "CurriculumId",
                table: "Classroom",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClassroomStudent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, comment: "Identifier for the student"),
                    ClassroomId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomStudent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomStudents_Classroom",
                        column: x => x.ClassroomId,
                        principalTable: "Classroom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomStudent_ClassroomId",
                table: "ClassroomStudent",
                column: "ClassroomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassroomStudent");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                table: "Classroom");

            migrationBuilder.CreateTable(
                name: "ClassroomResource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassroomId = table.Column<int>(type: "integer", nullable: false, comment: "Foreign key to classroom"),
                    CourseId = table.Column<int>(type: "integer", nullable: false, comment: "Foreign key to course")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomResources_Classroom",
                        column: x => x.ClassroomId,
                        principalTable: "Classroom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomResources_ClassroomId",
                table: "ClassroomResource",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomResources_ClassroomResource_Unique",
                table: "ClassroomResource",
                columns: new[] { "ClassroomId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomResources_ResourceId",
                table: "ClassroomResource",
                column: "CourseId");
        }
    }
}
