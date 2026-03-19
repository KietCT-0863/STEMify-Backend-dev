using FluentValidation;

namespace Identity.Application.Groups.Commands.AddStudentsToGroup;

public class AddStudentsToGroupCommandValidator : AbstractValidator<AddStudentsToGroupCommand>
{
    public AddStudentsToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .GreaterThan(0)
            .WithMessage("Group ID must be greater than zero");

        RuleFor(x => x.StudentIds)
            .NotEmpty()
            .WithMessage("At least one student ID is required");

        RuleForEach(x => x.StudentIds)
            .NotEmpty()
            .WithMessage("Student ID cannot be empty");
    }
}

