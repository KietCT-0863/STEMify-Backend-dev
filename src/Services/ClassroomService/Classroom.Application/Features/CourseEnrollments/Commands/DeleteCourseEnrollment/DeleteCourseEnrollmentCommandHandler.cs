using Classroom.Application.Common.Interfaces;
using Classroom.Domain.Enums;
using MediatR;
using Shared.Exceptions;

namespace Classroom.Application.Features.CourseEnrollments.Commands.DeleteCourseEnrollment
{
    public class DeleteEnrollmentCommandHandler : IRequestHandler<DeleteCourseEnrollmentCommand, bool>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;

        public DeleteEnrollmentCommandHandler(IClassroomUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(
            DeleteCourseEnrollmentCommand request,
            CancellationToken cancellationToken
        )
        {
            var enrollment = await _unitOfWork.CourseEnrollments.FindByIdAsync(
                request.EnrollmentId,
                cancellationToken
            );
            if (enrollment == null)
            {
                throw new NotFoundException(
                    $"Enrollment with ID {request.EnrollmentId} not found."
                );
            }

            enrollment.Status = EnrollmentStatus.Unenrolled;
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0;
        }
    }
}
