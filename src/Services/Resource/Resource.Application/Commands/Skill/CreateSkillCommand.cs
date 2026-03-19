using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Skill
{
    public class CreateSkillCommand : IRequest<SkillResponse>
    {
        public string SkillName { get; set; }
    }

    public class CreateSkillCommandValidator : AbstractValidator<CreateSkillCommand>
    {
        public CreateSkillCommandValidator()
        {
            RuleFor(x => x.SkillName)
                .NotEmpty()
                .WithMessage("Skill name is required.")
                .MaximumLength(100)
                .WithMessage("Skill name must not exceed 100 characters.");
        }
    }
}
