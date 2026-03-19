using FluentValidation;
using MediatR;
using Shared.Helper;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentAssignment.Commands.CreateStudentAssignmentAttempt
{
    public class CreateStudentAssignmentAttemptCommand : IRequest<GrpcAssignmentAttemptResponse>
    {
        public int StudentAssignmentId { get; set; }
        public List<AssignmentQuestionAttemptCommand> AssignmentQuestionAttempts { get; set; } = [];
    }

    public class AssignmentQuestionAttemptCommand
    {
        public int AssignmentQuestionId { get; set; }
        public string? AnswerText { get; set; }
        public byte[]? AnswerFile { get; set; }
    }

    public class CreateStudentAssignmentAttemptCommandValidator : AbstractValidator<CreateStudentAssignmentAttemptCommand>
    {
        public CreateStudentAssignmentAttemptCommandValidator()
        {
            RuleFor(x => x.StudentAssignmentId)
                .GreaterThan(0).WithMessage("StudentAssignmentId must be greater than zero.");

            RuleFor(x => x.AssignmentQuestionAttempts)
                .NotNull().WithMessage("AssignmentQuestionAttempts is required.")
                .Must(list => list != null && list.Count > 0).WithMessage("At least one AssignmentQuestionAttempt is required.");

            RuleForEach(x => x.AssignmentQuestionAttempts).SetValidator(new AssignmentQuestionAttemptCommandValidator());
        }

        private class AssignmentQuestionAttemptCommandValidator : AbstractValidator<AssignmentQuestionAttemptCommand>
        {
            private const int MaxImageBytes = 10 * 1024 * 1024;

            public AssignmentQuestionAttemptCommandValidator()
            {
                RuleFor(x => x.AssignmentQuestionId)
                    .GreaterThan(0).WithMessage("AssignmentQuestionId must be greater than zero.");

                // Require either AnswerText or AnswerFile (non-empty)
                RuleFor(x => x)
                    .Must(x => !string.IsNullOrWhiteSpace(x.AnswerText) || (x.AnswerFile != null && x.AnswerFile.Length > 0))
                    .WithMessage("Either AnswerText or AnswerFile must be provided for each question attempt.");

                When(x => x.AnswerText != null, () =>
                {
                    RuleFor(x => x.AnswerText)
                        .MaximumLength(2000).WithMessage("AnswerText must be 2000 characters or fewer.");
                });

                When(x => x.AnswerFile != null, () =>
                {
                    RuleFor(x => x.AnswerFile)
                        .Must(bytes => bytes != null && bytes.Length > 0)
                        .WithMessage("File is required.")
                        .Must(bytes => bytes != null && bytes.Length <= MaxImageBytes)
                        .WithMessage($"File must not exceed {MaxImageBytes / 1024 / 1024} MB.")
                        .Must(bytes => FileTypeHelper.IsDocument(bytes!) || FileTypeHelper.IsVideo(bytes!) || FileTypeHelper.IsImage(bytes!))
                        .WithMessage("File must be a valid document, image, or video format.");
                });
            }
        }
    }
}
