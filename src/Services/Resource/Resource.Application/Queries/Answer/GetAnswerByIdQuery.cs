using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Answer
{
    public class GetAnswerByIdQuery : IRequest<AnswerResponse>
    {
        public int Id { get; set; }

        public GetAnswerByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetAnswerByIdQueryValidator : AbstractValidator<GetAnswerByIdQuery>
    {
        public GetAnswerByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
