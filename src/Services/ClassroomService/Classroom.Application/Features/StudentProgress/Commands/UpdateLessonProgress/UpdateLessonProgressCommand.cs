using Classroom.Application.Models.ProgressModels;
using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Commands.UpdateLessonProgress
{
    public class UpdateLessonProgressCommand : IRequest<StudentLessonProgressModel>
    {
        public int LessonProgressId { get; set; }
        public ProgressStatus Status { get; set; }
    }
}
