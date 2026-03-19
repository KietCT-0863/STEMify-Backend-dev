using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Standard
{
    public class GetStandardByIdQuery : IRequest<StandardResponse>
    {
        public int Id { get; set; }

        public GetStandardByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetStandardByIdQueryValidator : AbstractValidator<GetStandardByIdQuery>
    {
        public GetStandardByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
