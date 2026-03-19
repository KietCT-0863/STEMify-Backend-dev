using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Models.EnrollmentModels;
using Infrastructure.Abstractions.Paging;
using Infrastructure.Common.Paging;
using MediatR;

namespace Classroom.Application.Features.CourseEnrollments.Queries.GetCourseEnrollmentList
{
    public class GetCourseEnrollmentListQueryHandler
        : IRequestHandler<GetCourseEnrollmentListQuery, PageList<CourseEnrollmentModel>>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ICourseCacheService _courseCache;

        public GetCourseEnrollmentListQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            ICourseCacheService courseCacheService
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _courseCache =
                courseCacheService ?? throw new ArgumentNullException(nameof(courseCacheService));
        }

        public async Task<PageList<CourseEnrollmentModel>> Handle(
            GetCourseEnrollmentListQuery request,
            CancellationToken cancellationToken
        )
        {
            var param = request.CourseEnrollmentParams;

            var enrollments = await _unitOfWork.CourseEnrollments.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = param.PageNumber,
                    PageSize = param.PageSize,
                },

                sortExpression: e => e.EnrolledAt,
                predicate: e =>
                    (!param.StudentId.HasValue || e.StudentId == param.StudentId)
                    && (!param.CourseId.HasValue || e.CourseId == param.CourseId)
                    && (string.IsNullOrEmpty(param.VerificationCode) || (e.Certificate != null && e.Certificate.VerificationCode == param.VerificationCode))
                    && (
                        param.Status.HasValue
                            ? e.Status == param.Status.Value
                            : e.Status != Domain.Enums.EnrollmentStatus.Unenrolled
                    ),
                cancellationToken: cancellationToken
            );

            // Get distinct CourseIds from the enrollments
            var courseIds = enrollments.Items.Select(r => r.CourseId).Distinct().ToList();

            var courseTasks = courseIds.ToDictionary(
                id => id,
                id => _courseCache.GetByIdAsync(id, cancellationToken)
            );

            await Task.WhenAll(courseTasks.Values);

            var courses = courseTasks
                .Where(x => x.Value.Result != null)
                .ToDictionary(x => x.Key, x => x.Value.Result);

            // Map to tasks
            var enrollmentModels = new List<CourseEnrollmentModel>();
            foreach (var enrollment in enrollments.Items)
            {
                var course = courses.GetValueOrDefault(enrollment.CourseId);
                var certificate = await _unitOfWork.Certificates.FindOneAsync(
                    c => c.CourseEnrollmentId == enrollment.Id,
                    cancellationToken
                );
                enrollmentModels.Add(enrollment.ToEnrollmentModel(course, certificate));
            }

            // Create a new PageList<CourseEnrollmentModel>
            var result = new PageList<CourseEnrollmentModel>(
                enrollmentModels,
                enrollments.PageNumber,
                enrollments.PageSize,
                enrollments.TotalCount
            );

            return result;
        }
    }
}
