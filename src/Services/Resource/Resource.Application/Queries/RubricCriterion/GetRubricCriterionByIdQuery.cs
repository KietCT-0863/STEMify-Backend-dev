using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.RubricCriterion
{
    public class GetRubricCriterionByIdQuery : IRequest<RubricCriterionResponse>
    {
        public int Id { get; set; }

        public GetRubricCriterionByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetRubricCriterionByIdQueryValidator : AbstractValidator<GetRubricCriterionByIdQuery>
    {
        public GetRubricCriterionByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
