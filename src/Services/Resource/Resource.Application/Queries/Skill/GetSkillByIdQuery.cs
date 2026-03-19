using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Skill
{
    public class GetSkillByIdQuery : IRequest<SkillResponse>
    {
        public int Id { get; set; }

        public GetSkillByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetSkillByIdQueryValidator : AbstractValidator<GetSkillByIdQuery>
    {
        public GetSkillByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
