using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Course
{
    public class GetCourseByIdQuery : IRequest<CourseDetail>
    {
        public int Id { get; set; }

        public GetCourseByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetCourseByIdQueryValidator : AbstractValidator<GetCourseByIdQuery>
    {
        public GetCourseByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
