using FluentValidation;
using MediatR;

namespace Classroom.Application.Features.StudentAssignment.Commands.CreateStudentAssignment
{
    public class CreateStudentAssignmentCommand : IRequest
    {
        public int AssignmentId { get; set; }
        public string StudentId { get; set; }
        public int StudentSectionProgressId { get; set; }
        public DateTime? DueDate { get; set; }
        public int? MaxAttemptAllowed { get; set; }
    }

    public class CreateStudentAssignmentCommandValidator : AbstractValidator<CreateStudentAssignmentCommand>
    {
        public CreateStudentAssignmentCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .GreaterThan(0).WithMessage("AssignmentId must be greater than zero.");

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("StudentId is required.");

            RuleFor(x => x.StudentSectionProgressId)
                .GreaterThan(0).WithMessage("StudentSectionProgressId must be greater than zero.");

            When(x => x.DueDate.HasValue, () =>
            {
                RuleFor(x => x.DueDate!.Value)
                    .Must(d => d.ToUniversalTime() >= DateTime.UtcNow.AddMinutes(-1))
                    .WithMessage("DueDate, when provided, must be in the present or future (UTC).");
            });

            When(x => x.MaxAttemptAllowed.HasValue, () =>
            {
                RuleFor(x => x.MaxAttemptAllowed!.Value)
                    .GreaterThan(0).WithMessage("MaxAttemptAllowed must be greater than zero when provided.");
            });
        }
    }
}