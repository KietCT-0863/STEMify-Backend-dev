using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.AgeRange
{
    public class GetAgeRangeByIdQuery : IRequest<AgeRangeResponse>
    {
        public int Id { get; set; }

        public GetAgeRangeByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetAgeRangeByIdQueryValidator : AbstractValidator<GetAgeRangeByIdQuery>
    {
        public GetAgeRangeByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
