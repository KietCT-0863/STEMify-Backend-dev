using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Section
{
    public class GetSectionByIdQuery : IRequest<SectionResponse>
    {
        public int Id { get; set; }

        public GetSectionByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetSectionByIdQueryValidator : AbstractValidator<GetSectionByIdQuery>
    {
        public GetSectionByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
