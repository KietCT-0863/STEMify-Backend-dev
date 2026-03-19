using Classroom.Application.Features.Classrooms.Commands.CreateClassroom;
using Classroom.Application.Features.Classrooms.Commands.UpdateClassroom;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Specifications.Classrooms;
using Shared.Extensions;
using Shared.Helper;
using Shared.Protos.Classroom;

namespace Classroom.Application.Extensions.Mapping
{
    public static class ClassroomMappingExtension
    {
        // Mapping from Domain model to Application Model
        public static ClassroomModel ToClassroomModel(this Domain.Entities.Classroom classroom)
        {
            if (classroom == null)
            {
                throw new ArgumentNullException(nameof(classroom), "Classroom cannot be null");
            }
            return new ClassroomModel
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = classroom.Description,
                CreatedAt = classroom.CreatedAt,
                UpdatedAt = classroom.UpdatedAt,
                ClassCode = classroom.ClassCode,
                CoverImageUrl = classroom.CoverImageUrl,
                EndDate = classroom.EndDate,
                Grade = classroom.Grade,
                StartDate = classroom.StartDate,
                Status = classroom.Status.ToString() == "Pending" ? "Upcoming" : classroom.Status.ToString(),
                OrganizationSubscriptionOrderId = classroom.OrganizationSubscriptionOrderId,
                OrganizationId = classroom.OrganizationId,
            };
        }

        // Mapping from application model to domain model
        public static Domain.Entities.Classroom ToClassroomEntity(
            this CreateClassroomCommand command
        )
        {
            if (command == null)
            {
                throw new ArgumentNullException(
                    nameof(CreateClassroomCommand),
                    "CreateClassroomCommand cannot be null"
                );
            }
            return new Domain.Entities.Classroom
            {
                Description = command.Description,
                CourseId = command.CourseId,
                EndDate = command.EndDate,
                StartDate = command.StartDate,
                Status = command.StartDate == DateOnly.FromDateTime(DateTime.Today)
                        ? Domain.Enums.ClassroomStatus.InProgress
                        : Domain.Enums.ClassroomStatus.Pending,
                OrganizationSubscriptionOrderId = command.OrganizationSubscriptionOrderId,
            };
        }

        public static void PatchFromCommand(
            this Domain.Entities.Classroom entity,
            UpdateClassroomCommand command
        )
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!string.IsNullOrEmpty(command.Name))
                entity.Name = command.Name;

            if (!string.IsNullOrEmpty(command.Grade))
                entity.Grade = command.Grade;

            if (!string.IsNullOrEmpty(command.Description))
                entity.Description = command.Description;

            if (command.StartDate.HasValue)
            {
                entity.StartDate = command.StartDate.Value;
                entity.Status = command.StartDate.Value == DateOnly.FromDateTime(DateTime.Today)
                    ? Domain.Enums.ClassroomStatus.InProgress
                    : Domain.Enums.ClassroomStatus.Pending;
            }

            if (command.EndDate.HasValue)
                entity.EndDate = command.EndDate.Value;

            if (command.TeacherId != null && command.TeacherId != Guid.Empty)
                entity.TeacherId = command.TeacherId.Value;
            if (command.CourseId.HasValue)
                entity.CourseId = command.CourseId.Value;
            if(!string.IsNullOrEmpty(command.ClassCode))
                entity.ClassCode = command.ClassCode;

            entity.UpdatedAt = DateTime.UtcNow; // Update the timestamp on modification
        }

        // Map result domain → gRPC
        public static GrpcClassroomResponse ToGrpcClassroomModel(this ClassroomModel classroomModel)
        {
            if (classroomModel == null)
            {
                throw new ArgumentNullException(
                    nameof(classroomModel),
                    "ClassroomModel cannot be null"
                );
            }
            var response = new GrpcClassroomResponse
            {
                Id = classroomModel.Id,
                Name = classroomModel.Name,
                Grade = classroomModel.Grade,
                Description = classroomModel.Description,
                CreatedAt = classroomModel.CreatedAt.ToString(),
                UpdatedAt = classroomModel.UpdatedAt.ToString(),
                StartDate = classroomModel.StartDate.ToString(),
                EndDate = classroomModel.EndDate.ToString(),
                ClassCode = classroomModel.ClassCode,
                CoverImageUrl = classroomModel.CoverImageUrl,
                Status = classroomModel.Status == "Pending" ? "Upcoming" : classroomModel.Status,
                OrganizationId = classroomModel.OrganizationId,
                OrganizationSubscriptionOrderId = classroomModel.OrganizationSubscriptionOrderId,
                Teacher = new GrpcUserModel
                {
                    Id = classroomModel.Teacher.UserId,
                    Name = classroomModel.Teacher.Name,
                    ImageUrl = classroomModel.Teacher.ImageUrl,
                    Email = classroomModel.Teacher.Email,
                },
                NumberOfStudents = classroomModel.NumberOfStudents,
                Students =
                {
                    classroomModel.Students.Select(s => new GrpcUserModel
                    {
                        Id = s.UserId,
                        Name = s.Name,
                        ImageUrl = s.ImageUrl,
                        Email = s.Email,
                    }),
                },
            };
            if (classroomModel.Course is not null)
            {
                response.Course = new GrpcCourseModel
                {
                    Id = classroomModel.Course.Id,
                    Title = classroomModel.Course.Title ?? string.Empty,
                    Description = classroomModel.Course.Description ?? string.Empty,
                    ImageUrl = classroomModel.Course.ImageUrl ?? string.Empty,
                    Code = classroomModel.Course.Code ?? string.Empty,
                    TotalDuration = classroomModel.Course.Duration,
                    LessonCount = classroomModel.Course.Lessons?.Count ?? 0
                };
            }
            return response;
        }

        // Map request gRPC → domain params
        public static ClassroomParams ToClassroomParams(this GetClassroomsRequest request)
        {
            return new ClassroomParams
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Search = request.Search,
                OrderBy = request.OrderBy,
                CourseId = request.CourseId,
                TeacherId =
                    string.IsNullOrWhiteSpace(request.TeacherId) ? null
                    : Guid.TryParse(request.TeacherId, out var guid) ? guid
                    : throw new FormatException("TeacherId is not a valid GUID"),
                Status = request.Status,
                FromDate = request.FromDate?.ToDateTime(),
                ToDate = request.ToDate?.ToDateTime(),
                OrganizationId = request.OrganizationId,
                StudentId = request.StudentId,
                OrganizationSubscriptionOrderId = request.OrganizationSubscriptionOrderId
            };
        }

        public static CreateClassroomCommand ToCreateClassroomCommand(
            this CreateClassroomRequest request
        )
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request),
                    "CreateClassroomRequest cannot be null"
                );
            }
            var classroomCommand = new CreateClassroomCommand
            {
                Description = request.Description,
                StartDate = request.StartDate.ToDateOnly(),
                EndDate = request.EndDate.ToDateOnly(),
                CoverImageUrl = request.CoverImageUrl,
                CourseId = request.CourseId,
                OrganizationSubscriptionOrderId = request.OrganizationSubscriptionOrderId,
            };
            foreach (var group in request.StudentGroups)
            {
                var studentGroup = new Features.Classrooms.Commands.CreateClassroom.StudentGroup
                {
                    GroupCode = group.GroupCode,
                    GroupName = group.GroupName,
                    TeacherId = Guid.TryParse(group.TeacherId, out var groupTeacherId)
                        ? groupTeacherId
                        : throw new FormatException("TeacherId in StudentGroup is not a valid GUID"),
                    StudentIds = group.StudentIds.ToList(),
                    Grade = group.Grade
                };
                classroomCommand.StudentGroups.Add(studentGroup);
            }
            return classroomCommand;
        }

        public static UpdateClassroomCommand ToUpdateClassroomCommand(
            this UpdateClassroomRequest request
        )
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request),
                    "UpdateClassroomRequest cannot be null"
                );
            }
            return new UpdateClassroomCommand
            {
                Id = request.Id,
                Name = request.Name,
                Grade = request.Grade,
                ClassCode = request.ClassCode,
                Description = request.Description,
                StartDate = request.StartDate != null ? request.StartDate.ToDateOnly() : null,
                EndDate = request.EndDate != null ? request.EndDate.ToDateOnly() : null,
                CoverImageUrl = request.CoverImageUrl,
                CourseId = request.CourseId,
                TeacherId = request.TeacherId != null
                    ? Guid.TryParse(request.TeacherId, out var teacherId)
                        ? teacherId
                        : throw new FormatException("TeacherId is not a valid GUID")
                    : null
            };
        }
    }
}
