using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Application.Specifications.Classrooms;
using MediatR;
using Shared.Exceptions;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomById
{
    public class GetClassroomByIdQueryHandler
        : IRequestHandler<GetClassroomByIdQuery, ClassroomModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _userClient;
        private readonly ICourseCacheService _courseCache;
        public GetClassroomByIdQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcUserClient userClient,
            ICourseCacheService courseCache
        )
        {
            _userClient = userClient ?? throw new ArgumentNullException(nameof(userClient));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _courseCache = courseCache;
        }

        public async Task<ClassroomModel> Handle(
            GetClassroomByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var classroom = await _unitOfWork.Classrooms.FirstOrDefaultAsync(
                new ClassroomByIdSpecification(request.Id),
                cancellationToken
            );

            if (classroom == null)
            {
                throw new NotFoundException($"Classroom with ID {request.Id} not found.");
            }

            // get teacher from cache, if not found in cache, it will call gRPC to identity service
            var teacher = await _userClient.GetOrganizationUserByIdAsync(classroom.TeacherId, cancellationToken);
            var course = await _courseCache.GetByIdAsync(classroom.CourseId, cancellationToken);
            var students = await Task.WhenAll(
                classroom.ClassroomStudents.Select(async cs =>
                {
                    var organizationUserInfo =
                        await _userClient.GetOrganizationUserByIdAsync(
                            Guid.Parse(cs.StudentId),
                            cancellationToken);

                    return new UserModel
                    {
                        UserId = organizationUserInfo.OrganizationUserId,
                        Email = organizationUserInfo.Email,
                        Name = organizationUserInfo.FullName
                    };
                })
            );

            var classroomResponseModel = classroom.ToClassroomModel();
            classroomResponseModel.Teacher = new UserModel
            {
                UserId = teacher.OrganizationUserId,
                Email = teacher.Email,
                Name = teacher.FullName
            };
            classroomResponseModel.Course = course;
            classroomResponseModel.NumberOfStudents = classroom.ClassroomStudents.Count;
            classroomResponseModel.Students = students;

            return classroomResponseModel;
        }
    }
}
