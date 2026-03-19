using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentAssignmentTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz");

            migrationBuilder.DropIndex(
                name: "IX_StudentAssignment_StudentSectionProgressId",
                table: "StudentAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_StudentSectionProgressId",
                table: "StudentAssignments",
                column: "StudentSectionProgressId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz",
                column: "StudentSectionProgressId",
                principalTable: "StudentSectionProgress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz");

            migrationBuilder.DropIndex(
                name: "IX_StudentAssignment_StudentSectionProgressId",
                table: "StudentAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_StudentSectionProgressId",
                table: "StudentAssignments",
                column: "StudentSectionProgressId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz",
                column: "StudentSectionProgressId",
                principalTable: "StudentSectionProgress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
