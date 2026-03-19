using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Exporter
{
    public class GetExportedCourse : IRequest<ExportCourseResponse>
    {
        public int Id { get; set; }

        public GetExportedCourse(int id)
        {
            Id = id;
        }
    }

    public class GetExportedCourseValidator : AbstractValidator<GetExportedCourse>
    {
        public GetExportedCourseValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
