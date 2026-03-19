using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Skill
{
    public class UpdateSkillCommand : IRequest<SkillResponse>
    {
        public int Id { get; set; }
        public string SkillName { get; set; }
    }

    public class UpdateSkillCommandValidator : AbstractValidator<UpdateSkillCommand>
    {
        public UpdateSkillCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");

            RuleFor(x => x.SkillName)
                .NotEmpty()
                .WithMessage("Skill name is required.")
                .MaximumLength(100)
                .WithMessage("Skill name must not exceed 100 characters.");
        }
    }
}
