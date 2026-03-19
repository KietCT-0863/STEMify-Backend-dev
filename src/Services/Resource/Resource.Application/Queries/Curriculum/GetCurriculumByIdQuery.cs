using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Curriculum
{
    public class GetCurriculumByIdQuery : IRequest<CurriculumDetails>
    {
        public int Id { get; set; }

        public GetCurriculumByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetCurriculumByIdQueryValidator : AbstractValidator<GetCurriculumByIdQuery>
    {
        public GetCurriculumByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
