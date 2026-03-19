using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Queries.Classrooms;
using Classroom.Application.Specifications.Classrooms;
using Infrastructure.Abstractions.Paging;
using MediatR;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomList
{
    public class GetClassroomListQueryHandler
        : IRequestHandler<GetClassroomListQuery, PageList<ClassroomModel>>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _userClient;
        private readonly ICourseCacheService _courseCache;

        public GetClassroomListQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcUserClient userClient,
            ICourseCacheService courseCache)
        {
            _unitOfWork = unitOfWork;
            _userClient = userClient;
            _courseCache = courseCache;
        }
        public async Task<PageList<ClassroomModel>> Handle(
            GetClassroomListQuery request,
            CancellationToken cancellationToken
        )
        {
            var param = request.ClassroomParams;
            var spec = new ClassroomSpecification(param);

            var classrooms = await _unitOfWork.Classrooms.GetAllAsync(spec, cancellationToken);
            var totalCount = await _unitOfWork.Classrooms.CountAsync(spec, cancellationToken);

            var classroomList = classrooms.Select(c => c.ToClassroomModel()).ToList();
            var tasks = classroomList.Select(async classroom =>
            {
                // Lấy TeacherId và CourseId từ entity tương ứng
                var entity = classrooms.FirstOrDefault(e => e.Id == classroom.Id);
                if (entity != null)
                {
                    var orgUserInfo = await _userClient.GetOrganizationUserByIdAsync(entity.TeacherId, cancellationToken);
                    classroom.Teacher = new Models.EnrollmentModels.UserModel
                    {
                        UserId = orgUserInfo.UserId,
                        Email = orgUserInfo.Email,
                        Name = orgUserInfo.FullName
                    };
                    classroom.Course = await _courseCache.GetByIdAsync(entity.CourseId, cancellationToken);
                    classroom.NumberOfStudents = entity.ClassroomStudents.Count;
                }
            });

            await Task.WhenAll(tasks);

            var result = new PageList<ClassroomModel>(
                classroomList,
                param.PageNumber,
                param.PageSize,
                totalCount
            );
            return result;
        }
    }
}
