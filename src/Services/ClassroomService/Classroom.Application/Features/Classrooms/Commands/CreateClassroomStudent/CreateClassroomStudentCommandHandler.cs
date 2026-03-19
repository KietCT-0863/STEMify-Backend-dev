using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Domain.Entities;
using Contracts.Abstractions.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Order;

namespace Classroom.Application.Features.Classrooms.Commands.CreateClassroomStudent
{
    public class CreateClassroomStudentCommandHandler : IRequestHandler<CreateClassroomStudentCommand, Unit>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcOrganizationSubscriptionOrderClient _grpcOrderClient;
        private readonly IGrpcUserClient _grpcUserClient;
        private readonly ILogger<CreateClassroomStudentCommandHandler> _logger;
        public CreateClassroomStudentCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcOrganizationSubscriptionOrderClient grpcOrderClient,
            IGrpcUserClient grpcUserClient,
            ILogger<CreateClassroomStudentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _grpcOrderClient = grpcOrderClient;
            _grpcUserClient = grpcUserClient;
            _logger = logger;

        }
        public async Task<Unit> Handle(CreateClassroomStudentCommand request, CancellationToken cancellationToken)
        {
            var classroom = await _unitOfWork.Classrooms.FindByIdAsync(request.ClassroomId, cancellationToken);
            if (classroom == null)
            {
                throw new NotFoundException("Classroom not found");
            }

            // get organization subscription
            var subscription = await _grpcOrderClient.GetOrganizationSubscriptionByIdAsync(classroom.OrganizationSubscriptionOrderId);

            List<ClassroomStudent> classroomStudents = [];
            List<string> studentIds = (request.StudentEmails != null && request.StudentEmails.Count > 0)
                ? await GetUserIdsByEmails(request.StudentEmails)
                : request.StudentIds ?? new();
            if (!studentIds.Any())
            {
                return Unit.Value;
            }

            var existingStudentIds = (await _unitOfWork.ClassroomStudents
               .FindAsync(c => c.ClassroomId == request.ClassroomId && studentIds.Contains(c.StudentId), cancellationToken: cancellationToken))
               .Select(c => c.StudentId).ToList();
            var existingStudentSet = existingStudentIds.ToHashSet();


            var classroomStudentsToAdd = new List<ClassroomStudent>();
            var licenseAssignmentsRequest = new CreateLicenseAssignmentsRequest();
            var studentsNeedLicense = new List<string>();

            foreach (var studentId in studentIds)
            {
                // validate student not exist in the classroom
                if (existingStudentSet.Contains(studentId))
                {
                    _logger.LogInformation(
                        "Student {StudentId} already exists in classroom {ClassroomId}",
                        studentId, request.ClassroomId);
                    continue;
                }

                classroomStudentsToAdd.Add(new ClassroomStudent
                {
                    ClassroomId = request.ClassroomId,
                    StudentId = studentId,
                    JoinedAt = DateTime.UtcNow
                });

                // validate student has license in the subscription
                if (!subscription.LicenseAssignmentUserIds.Contains(studentId))
                {
                    _logger.LogInformation($"Student {studentId} does not have license in subscription");
                    studentsNeedLicense.Add(studentId);
                    var licenseAssignmentModel = new CreateLicenseAssignmentRequest
                    {
                        OrganizationSubscriptionOrderId = classroom.OrganizationSubscriptionOrderId,
                        UserId = studentId.ToString(),
                        Type = "Student"
                    };
                    licenseAssignmentsRequest.LicenseAssignments.Add(licenseAssignmentModel);
                }
            }

            if (!classroomStudentsToAdd.Any())
            {
                return Unit.Value;
            }

            // check available student licenses
            if (studentsNeedLicense.Any())
            {
                var availableLicenses =
                    subscription.MaxStudentSeats -
                    subscription.CurrentStudentSeats;

                if (studentsNeedLicense.Count > availableLicenses)
                {
                    throw new ArgumentException(
                        "Không đủ license dành cho học sinh trong gói đăng ký.");
                }
            }
            // 8. Create license assignments (if needed)
            if (licenseAssignmentsRequest.LicenseAssignments.Any())
            {
                await _grpcOrderClient
                    .CreateLicenseAssignmentAssignmentsAsync(
                        licenseAssignmentsRequest);
            }

            // 9. Add classroom students
            await _unitOfWork.ClassroomStudents
                .AddRangeAsync(classroomStudentsToAdd, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        private async Task<List<string>> GetUserIdsByEmails(List<string> studentEmails)
        {
            var users = await _grpcUserClient.GetUsersByEmailsAsync(studentEmails);
            var userIds = users.Select(u => u.UserId).ToList();

            return userIds;
        }
    }
}
