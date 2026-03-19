using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Quiz
{
    public class GetQuizByIdQuery : IRequest<QuizResponse>
    {
        public int Id { get; set; }

        public GetQuizByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetQuizByIdQueryValidator : AbstractValidator<GetQuizByIdQuery>
    {
        public GetQuizByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
