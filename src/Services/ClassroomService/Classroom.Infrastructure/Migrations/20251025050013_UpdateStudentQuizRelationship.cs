using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentQuizRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuiz_StudentSectionProgressId",
                table: "StudentQuiz");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuiz_StudentSectionProgressId",
                table: "StudentQuiz",
                column: "StudentSectionProgressId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz",
                column: "StudentSectionProgressId",
                principalTable: "StudentSectionProgress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuiz_StudentSectionProgressId",
                table: "StudentQuiz");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuiz_StudentSectionProgressId",
                table: "StudentQuiz",
                column: "StudentSectionProgressId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                table: "StudentQuiz",
                column: "StudentSectionProgressId",
                principalTable: "StudentSectionProgress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
