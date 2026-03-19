using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Curriculum
{
    public class GetCurriculumRelationsQuery : IRequest<CurriculumRelationsResponse>
    {
        public int Id { get; set; }

        public GetCurriculumRelationsQuery(int id)
        {
            Id = id;
        }
    }

    public class GetCurriculumRelationsQueryValidator : AbstractValidator<GetCurriculumRelationsQuery>
    {
        public GetCurriculumRelationsQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
