using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentAssignmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentSectionProgressId = table.Column<int>(type: "integer", nullable: false),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, comment: "Final score as percentage (0-100)"),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MaxAttemptAllowed = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAssignments_StudentSectionProgress_StudentSectionPro~",
                        column: x => x.StudentSectionProgressId,
                        principalTable: "StudentSectionProgress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    TeacherId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m, comment: "Total score as percentage (0-100)"),
                    Feedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UnderReview")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentAttempts_StudentAssignments_StudentAssignmentId",
                        column: x => x.StudentAssignmentId,
                        principalTable: "StudentAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentQuestionAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentAttemptId = table.Column<int>(type: "integer", nullable: false),
                    AssignmentQuestionId = table.Column<int>(type: "integer", nullable: false),
                    AnswerText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true, comment: "Text answer provided by student"),
                    AnswerFileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "URL to uploaded answer file"),
                    Points = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m, comment: "Points earned for this question")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentQuestionAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentQuestionAttempts_AssignmentAttempts_AssignmentAtt~",
                        column: x => x.AssignmentAttemptId,
                        principalTable: "AssignmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentQuestionAttemptId = table.Column<int>(type: "integer", nullable: false),
                    RubricCriterionId = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m, comment: "Points awarded for this rubric criterion")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricScores_AssignmentQuestionAttempts_AssignmentQuestionA~",
                        column: x => x.AssignmentQuestionAttemptId,
                        principalTable: "AssignmentQuestionAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttempt_Status",
                table: "AssignmentAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttempt_StudentAssignmentId",
                table: "AssignmentAttempts",
                column: "StudentAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttempt_StudentAssignmentId_AttemptNumber",
                table: "AssignmentAttempts",
                columns: new[] { "StudentAssignmentId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttempt_SubmittedAt",
                table: "AssignmentAttempts",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttempt_TeacherId",
                table: "AssignmentAttempts",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttempt_TeacherId_Status",
                table: "AssignmentAttempts",
                columns: new[] { "TeacherId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentQuestionAttempt_AssignmentAttemptId",
                table: "AssignmentQuestionAttempts",
                column: "AssignmentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentQuestionAttempt_AssignmentAttemptId_QuestionId",
                table: "AssignmentQuestionAttempts",
                columns: new[] { "AssignmentAttemptId", "AssignmentQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentQuestionAttempt_AssignmentQuestionId",
                table: "AssignmentQuestionAttempts",
                column: "AssignmentQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricScore_AssignmentQuestionAttemptId",
                table: "RubricScores",
                column: "AssignmentQuestionAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricScore_QuestionAttemptId_CriterionId",
                table: "RubricScores",
                columns: new[] { "AssignmentQuestionAttemptId", "RubricCriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubricScore_RubricCriterionId",
                table: "RubricScores",
                column: "RubricCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_AssignmentId",
                table: "StudentAssignments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_DueDate",
                table: "StudentAssignments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_Status",
                table: "StudentAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_StudentId",
                table: "StudentAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_StudentId_AssignmentId",
                table: "StudentAssignments",
                columns: new[] { "StudentId", "AssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_StudentId_Status",
                table: "StudentAssignments",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignment_StudentSectionProgressId",
                table: "StudentAssignments",
                column: "StudentSectionProgressId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RubricScores");

            migrationBuilder.DropTable(
                name: "AssignmentQuestionAttempts");

            migrationBuilder.DropTable(
                name: "AssignmentAttempts");

            migrationBuilder.DropTable(
                name: "StudentAssignments");
        }
    }
}
