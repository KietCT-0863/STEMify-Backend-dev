using Classroom.Application.Models.ProgressModels;
using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Commands.UpdateSectionProgress
{
    public class UpdateSectionProgressCommand : IRequest<StudentSectionProgressModel>
    {
        public int EnrollmentId { get; set; }
        public int LessonId { get; set; }
        public int SectionId { get; set; }
        public ProgressStatus Status { get; set; }
        public int? SectionProgressId { get; set; }
    }
}
