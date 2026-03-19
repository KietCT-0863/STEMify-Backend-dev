using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentQuizTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentQuiz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizId = table.Column<int>(type: "integer", nullable: false),
                    StudentSectionProgressId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinalScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxAttemptAllowed = table.Column<int>(type: "integer", nullable: true),
                    TimeLimitMinutes = table.Column<int>(type: "integer", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuiz", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentQuiz_StudentSectionProgress_StudentSectionProgressId",
                        column: x => x.StudentSectionProgressId,
                        principalTable: "StudentSectionProgress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentQuizId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TotalScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAttempt_StudentQuiz_StudentQuizId",
                        column: x => x.StudentQuizId,
                        principalTable: "StudentQuiz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestionAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizAttemptId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestionAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizQuestionAttempt_QuizAttempt_QuizAttemptId",
                        column: x => x.QuizAttemptId,
                        principalTable: "QuizAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionAttemptId = table.Column<int>(type: "integer", nullable: false),
                    AnswerId = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerAttempt_QuizQuestionAttempt_QuestionAttemptId",
                        column: x => x.QuestionAttemptId,
                        principalTable: "QuizQuestionAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerAttempt_QuestionAttempt",
                table: "AnswerAttempt",
                column: "QuestionAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerAttempt_QuestionAttempt_Answer",
                table: "AnswerAttempt",
                columns: new[] { "QuestionAttemptId", "AnswerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempt_StudentQuiz",
                table: "QuizAttempt",
                column: "StudentQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempt_StudentQuiz_AttemptNumber",
                table: "QuizAttempt",
                columns: new[] { "StudentQuizId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestionAttempt_QuizAttempt",
                table: "QuizQuestionAttempt",
                column: "QuizAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestionAttempt_QuizAttempt_Question",
                table: "QuizQuestionAttempt",
                columns: new[] { "QuizAttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuiz_Quiz_SectionProgress",
                table: "StudentQuiz",
                columns: new[] { "QuizId", "StudentSectionProgressId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuiz_StudentSectionProgressId",
                table: "StudentQuiz",
                column: "StudentSectionProgressId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerAttempt");

            migrationBuilder.DropTable(
                name: "QuizQuestionAttempt");

            migrationBuilder.DropTable(
                name: "QuizAttempt");

            migrationBuilder.DropTable(
                name: "StudentQuiz");
        }
    }
}
