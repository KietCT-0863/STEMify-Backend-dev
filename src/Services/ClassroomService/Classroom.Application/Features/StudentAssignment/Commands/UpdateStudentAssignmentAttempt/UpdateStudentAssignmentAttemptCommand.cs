using FluentValidation;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentAssignment.Commands.UpdateStudentAssignmentAttempt
{
    public class UpdateStudentAssignmentAttemptCommand : IRequest<GrpcAssignmentAttemptResponse>
    {
        public int Id { get; set; }
        public string? Feedback { get; set; }
        public List<QuestionGradeCommand> Grades { get; set; } = new();
    }

    public class QuestionGradeCommand
    {
        public int AssignmentQuestionAttemptId { get; set; }
        public List<RubricScoreCommand> RubricScores { get; set; } = new();
    }

    public class RubricScoreCommand
    {
        public int RubricCriterionId { get; set; }
        public decimal Points { get; set; }
    }

    public class UpdateStudentAssignmentAttemptCommandValidator : AbstractValidator<UpdateStudentAssignmentAttemptCommand>
    {
        public UpdateStudentAssignmentAttemptCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than zero.");

            RuleFor(x => x.Grades)
                .NotNull().WithMessage("Grades is required.")
                .Must(list => list != null && list.Count > 0).WithMessage("At least one grade must be provided.");

            RuleForEach(x => x.Grades).SetValidator(new QuestionGradeCommandValidator());
        }

        private class QuestionGradeCommandValidator : AbstractValidator<QuestionGradeCommand>
        {
            public QuestionGradeCommandValidator()
            {
                RuleFor(x => x.AssignmentQuestionAttemptId)
                    .GreaterThan(0).WithMessage("AssignmentQuestionAttemptId must be greater than zero.");

                RuleFor(x => x.RubricScores)
                    .NotNull().WithMessage("RubricScores is required.")
                    .Must(list => list != null && list.Count > 0).WithMessage("At least one rubric score is required.");

                RuleForEach(x => x.RubricScores).SetValidator(new RubricScoreCommandValidator());
            }
        }

        private class RubricScoreCommandValidator : AbstractValidator<RubricScoreCommand>
        {
            public RubricScoreCommandValidator()
            {
                RuleFor(x => x.RubricCriterionId)
                    .GreaterThan(0).WithMessage("RubricCriterionId must be greater than zero.");

                RuleFor(x => x.Points)
                    .GreaterThanOrEqualTo(0m).WithMessage("Points must be greater than or equal to 0.");
            }
        }
    }
}