using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Lesson
{
    public class GetLessonByIdQuery : IRequest<LessonResponse>
    {
        public int Id { get; set; }

        public GetLessonByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetLessonByIdQueryValidator : AbstractValidator<GetLessonByIdQuery>
    {
        public GetLessonByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
