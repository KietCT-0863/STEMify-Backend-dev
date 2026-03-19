using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Commands.CreateSectionProgress
{
    public class CreateSectionProgressCommand : IRequest<Unit>
    {
        public int SectionId { get; set; }
        public int StudentLessonProgressId { get; set; }
        public ProgressStatus Status { get; set; }
    }
}
