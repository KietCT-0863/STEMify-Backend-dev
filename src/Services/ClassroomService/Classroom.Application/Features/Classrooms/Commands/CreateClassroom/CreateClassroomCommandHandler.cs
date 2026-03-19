using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Domain.Entities;
using DnsClient.Internal;
using Google.Protobuf;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Order;

namespace Classroom.Application.Features.Classrooms.Commands.CreateClassroom
{
    public class CreateClassroomCommandHandler
        : IRequestHandler<CreateClassroomCommand, GrpcCreateClassroomResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcCourseClient _courseClient;
        private readonly IGrpcOrganizationSubscriptionOrderClient _grpcOrderClient;
        private readonly ILogger<CreateClassroomCommandHandler> _logger;
        public CreateClassroomCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcCourseClient courseClient,
            IGrpcOrganizationSubscriptionOrderClient grpcOrderClient,
            ILogger<CreateClassroomCommandHandler> logger
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _courseClient = courseClient ?? throw new ArgumentNullException(nameof(courseClient));
            _grpcOrderClient = grpcOrderClient ?? throw new ArgumentNullException(nameof(grpcOrderClient));
            _logger = logger ?? throw new ArgumentNullException( nameof(logger));
        }

        public async Task<GrpcCreateClassroomResponse> Handle(
            CreateClassroomCommand request,
            CancellationToken cancellationToken
        )
        {
            if(request.StudentGroups.Count <= 0)
            {
                throw new ArgumentException("Danh sách nhóm học sinh không được rỗng.");
            }
            // Validate and fetch external resources if needed
            var course = await _courseClient.GetCourseByIdAsync(request.CourseId);
            var subscription = await _grpcOrderClient.GetOrganizationSubscriptionByIdAsync(request.OrganizationSubscriptionOrderId);

            // validate subscription status
            if (subscription.Status != "Pending" && subscription.Status != "Active")
            {
                throw new ArgumentException("Gói đăng ký của tổ chức hiện không hoạt động.");
            }
            // validate classroom dates within subscription period
            var classroomStartDate = request.StartDate;
            var classroomEndDate = request.EndDate;

            var subscriptionStartDate = DateOnly.FromDateTime(subscription.StartDate.ToDateTime());
            var subscriptionEndDate = DateOnly.FromDateTime(subscription.EndDate.ToDateTime());

            if (subscriptionStartDate > classroomStartDate || subscriptionEndDate < classroomEndDate)
            {
                throw new ArgumentException("Thời gian học của lớp phải nằm trong khoảng thời gian hiệu lực của gói đăng ký.");
            }

            List<Domain.Entities.Classroom> classroomEntities = new();

            // Convert the CreateClassroomModel to a Classroom entity
            foreach (var group in request.StudentGroups)
            {
                var classCode = $"{course.Code}-{group.GroupCode}";
                // Check xem classCode đã tồn tại chưa
                var isExist = await _unitOfWork.Classrooms
                    .AnyAsync(c => c.ClassCode == classCode, cancellationToken);
                if (isExist)
                {
                    throw new ArgumentException($"Nhóm học sinh '{group.GroupName}' đã được gán vào khóa học '{course.Title}'. Vui lòng chọn nhóm khác.");
                }

                List<Guid> teacherIdsToAssign = new();
                List<string> studentIdsToAssign = new();
                // Validate teacher license
                if (!subscription.LicenseAssignmentUserIds.Contains(group.TeacherId.ToString()))
                {
                    _logger.LogInformation($"Teacher {group.TeacherId} does not have license in subscription");
                    teacherIdsToAssign.Add(group.TeacherId);
                }

                // Validate student license
                foreach (var studentId in group.StudentIds)
                {
                    if (!subscription.LicenseAssignmentUserIds.Contains(studentId))
                    {
                        _logger.LogInformation($"Student {studentId} does not have license in subscription");
                        studentIdsToAssign.Add(studentId);
                    }
                }

                // Create license assignments if needed
                if (teacherIdsToAssign.Count > 0 || studentIdsToAssign.Count > 0)
                {
                    if(subscription.MaxStudentSeats - subscription.CurrentStudentSeats < studentIdsToAssign.Count)
                    {
                        _logger.LogWarning("Not enough student seats in subscription");
                        throw new ArgumentException("Số lượng học sinh vượt quá số lượng ghế học sinh còn lại trong gói đăng ký.");
                    }
                    if(subscription.MaxTeacherSeats - subscription.CurrentTeacherSeats < teacherIdsToAssign.Count)
                    {
                        _logger.LogWarning("Not enough teacher seats in subscription");
                        throw new ArgumentException("Số lượng giáo viên vượt quá số lượng ghế giáo viên còn lại trong gói đăng ký.");
                    }
                    var licenseAssignmentsRequest = new CreateLicenseAssignmentsRequest();
                    foreach(var teacherId in teacherIdsToAssign)
                    {
                        var licenseAssignmentModel = new CreateLicenseAssignmentRequest
                        {
                            OrganizationSubscriptionOrderId = request.OrganizationSubscriptionOrderId,
                            UserId = teacherId.ToString(),
                            Type = "Teacher"
                        };
                        licenseAssignmentsRequest.LicenseAssignments.Add(licenseAssignmentModel);
                    }
                    foreach (var studentId in studentIdsToAssign)
                    {
                        var licenseAssignmentModel = new CreateLicenseAssignmentRequest
                        {
                            OrganizationSubscriptionOrderId = request.OrganizationSubscriptionOrderId,
                            UserId = studentId.ToString(),
                            Type = "Student"
                        };
                        licenseAssignmentsRequest.LicenseAssignments.Add(licenseAssignmentModel);
                    }
                    var licenseResponse = await _grpcOrderClient.CreateLicenseAssignmentAssignmentsAsync(licenseAssignmentsRequest);
                    _logger.LogInformation($"Created {licenseResponse.LicenseAssignments.Count} license assignments for classroom creation.");
                }

                // Build Classroom per group
                var classroom = request.ToClassroomEntity();                 // map base fields
                classroom.OrganizationId = subscription.OrganizationId;
                classroom.TeacherId = group.TeacherId;
                classroom.Grade = group.Grade;
                classroom.ClassCode = $"{course.Code}-{group.GroupCode}";    // class unique code
                classroom.Name = $"{course.Code}-{group.GroupName}";

                classroom.ClassroomStudents = group.StudentIds.Select(id => new ClassroomStudent
                {
                    StudentId = id,
                    JoinedAt = DateTime.UtcNow
                }).ToList();

                classroomEntities.Add(classroom);
            }

            // Add classroom entity
            await _unitOfWork.Classrooms.AddRangeAsync(classroomEntities, cancellationToken);

            // save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var grpcResponse = new GrpcCreateClassroomResponse
            {
                Classrooms ={classroomEntities.Select(c => new GrpcCreateClassroomModel
                                {
                                    Id = c.Id,
                                    ClassCode = c.ClassCode,
                                    ClassName = c.Name,
                                })}
            };
            return grpcResponse;
        }
    }
}
