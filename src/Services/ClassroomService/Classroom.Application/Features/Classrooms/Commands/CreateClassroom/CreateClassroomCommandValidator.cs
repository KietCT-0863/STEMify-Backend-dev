using FluentValidation;

namespace Classroom.Application.Features.Classrooms.Commands.CreateClassroom
{
    public class CreateClassroomModelValidator : AbstractValidator<CreateClassroomCommand>
    {
        public CreateClassroomModelValidator()
        {

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required.")
                .Must(date => date.ToDateTime(TimeOnly.MinValue).Date >= DateTime.Now.Date)
                .WithMessage("Start date must be today or in the future.");

            RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.");

            RuleFor(x => x)
                .Must(x => x.StartDate < x.EndDate)
                .WithMessage("Start date must be earlier than end date.");
        }
    }
}
