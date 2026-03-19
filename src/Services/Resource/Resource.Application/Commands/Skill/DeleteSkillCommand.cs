using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Skill
{
    public class DeleteSkillCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteSkillCommandValidator : AbstractValidator<DeleteSkillCommand>
    {
        public DeleteSkillCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
