using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Exporter
{
    public class GetExportedLesson : IRequest<ExportLessonResponse>
    {
        public int Id { get; set; }

        public GetExportedLesson(int id)
        {
            Id = id;
        }
    }

    public class GetExportedLessonValidator : AbstractValidator<GetExportedLesson>
    {
        public GetExportedLessonValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
