using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.CourseLearningOutcome
{
    public class GetCourseLearningOutcomeByIdQuery : IRequest<CourseLearningOutcomeResponse>
    {
        public int Id { get; set; }

        public GetCourseLearningOutcomeByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetCourseLearningOutcomeByIdQueryValidator : AbstractValidator<GetCourseLearningOutcomeByIdQuery>
    {
        public GetCourseLearningOutcomeByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
