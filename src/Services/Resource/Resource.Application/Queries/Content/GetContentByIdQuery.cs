using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Content
{
    public class GetContentByIdQuery : IRequest<ContentResponse>
    {
        public int Id { get; set; }

        public GetContentByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetContentByIdQueryValidator : AbstractValidator<GetContentByIdQuery>
    {
        public GetContentByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
