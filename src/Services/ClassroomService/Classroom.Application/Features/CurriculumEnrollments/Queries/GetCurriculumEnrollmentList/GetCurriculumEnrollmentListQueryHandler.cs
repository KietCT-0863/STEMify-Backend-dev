using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Domain.Enums;
using Infrastructure.Abstractions.Paging;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Application.Features.CurriculumEnrollments.Queries.GetCurriculumEnrollmentList
{
    public class GetCurriculumEnrollmentListQueryHandler
        : IRequestHandler<GetCurriculumEnrollmentListQuery, PageList<CurriculumEnrollmentModel>>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ICurriculumCacheService _curriculumCache;

        public GetCurriculumEnrollmentListQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            ICurriculumCacheService curriculumCache
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _curriculumCache = curriculumCache ?? throw new ArgumentNullException(nameof(curriculumCache));
        }

        public async Task<PageList<CurriculumEnrollmentModel>> Handle(
             GetCurriculumEnrollmentListQuery request,
             CancellationToken cancellationToken
         )
        {
            var param = request.CurriculumEnrollmentParams;

            // 1️ Lấy danh sách CurriculumEnrollments theo paging và filter
            var enrollments = await _unitOfWork.CurriculumEnrollments.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = param.PageNumber,
                    PageSize = param.PageSize,
                },
                sortExpression: e => e.EnrolledAt,
                projectionFunc: e => e
                    .Include(c => c.Certificate)
                    .Include(c => c.CourseEnrollments)
                        .ThenInclude(c => c.Certificate),
                predicate: e =>
                    (!param.StudentId.HasValue || e.StudentId == param.StudentId) &&
                    (!param.CertificateId.HasValue ||
                        (e.Certificate != null && e.Certificate.Id == param.CertificateId)) &&
                    (string.IsNullOrEmpty(param.VerificationCode) ||
                        (e.Certificate != null && e.Certificate.VerificationCode == param.VerificationCode)) &&
                    (!param.CurriculumId.HasValue || e.CurriculumId == param.CurriculumId) &&
                    //(!param.OrganizationSubscriptionOrderId.HasValue || (e.Classroom != null && e.Classroom.OrganizationSubscriptionOrderId == param.OrganizationSubscriptionOrderId)) &&
                    //(!param.ClassroomId.HasValue || e.ClassroomId == param.ClassroomId) &&
                    (param.Status.HasValue
                        ? e.Status == param.Status.Value
                        : e.Status != EnrollmentStatus.Unenrolled),
                cancellationToken: cancellationToken
            );

            if (!enrollments.Items.Any())
            {
                return new PageList<CurriculumEnrollmentModel>(
                    new List<CurriculumEnrollmentModel>(),
                    enrollments.PageNumber,
                    enrollments.PageSize,
                    enrollments.TotalCount
                );
            }

            // 2️ Lấy dữ liệu curriculum từ cache song song
            var curriculumIds = enrollments.Items
                .Select(e => e.CurriculumId)
                .Distinct()
                .ToList();

            var curriculumTasks = curriculumIds.ToDictionary(
                id => id,
                id => _curriculumCache.GetByIdAsync(id, cancellationToken)
            );

            await Task.WhenAll(curriculumTasks.Values);

            // 3️ Dựng response cho từng curriculum enrollment
            var studentId = param.StudentId ?? Guid.Empty;
            var result = new List<CurriculumEnrollmentModel>();

            foreach (var enrollment in enrollments.Items)
            {
                var curriculum = await curriculumTasks[enrollment.CurriculumId];
                if (curriculum == null) continue;

                var curriculumCourses = curriculum.Courses ?? new List<CourseDetail>();
                var enrolledCourseDict = enrollment.CourseEnrollments
                    .Where(c => c.Status != EnrollmentStatus.Unenrolled)
                    .ToDictionary(c => c.CourseId, c => c);

                // 3 Duyệt toàn bộ course trong curriculum để gộp thông tin
                var courseEnrollments = new List<CourseEnrollmentModel>();
                foreach (var course in curriculumCourses)
                {
                    if (enrolledCourseDict.TryGetValue(course.Id, out var enrolled))
                    {
                        var cert = enrolled.Certificate;
                        courseEnrollments.Add(new CourseEnrollmentModel
                        {
                            Id = enrolled.Id,
                            CourseId = course.Id,
                            CourseTitle = course.Title,
                            CoverImageUrl = course.ImageUrl,
                            Description = course.Description,
                            StudentId = enrollment.StudentId.ToString(),
                            StudentName = enrollment.Certificate?.UserName ?? "",
                            EnrolledAt = enrolled.EnrolledAt,
                            CompletedAt = enrolled.CompletedAt,
                            Status = enrolled.Status.ToString(),
                            FinalScore = enrolled.FinalScore,
                            ProgressPercentage = enrolled.ProgressPercentage,
                            VerificationCode = cert?.VerificationCode,
                            CertificateId = cert?.Id,
                            CertificateUrl = cert?.CertificateUrl
                        });
                    }
                    else
                    {
                        //Course chưa enroll
                        courseEnrollments.Add(new CourseEnrollmentModel
                        {
                            CourseId = course.Id,
                            CourseTitle = course.Title,
                            CoverImageUrl = course.ImageUrl,
                            Description = course.Description,
                            StudentId = enrollment.StudentId.ToString(),
                            StudentName = enrollment.Certificate?.UserName ?? "",
                            Status = null,
                            EnrolledAt = null,
                            CompletedAt = null,
                            FinalScore = null,
                            ProgressPercentage = 0,
                            CertificateId = null,
                            VerificationCode = null,
                            CertificateUrl = null
                        });
                    }
                }

                // 3. Dựng CurriculumEnrollmentModel
                var certCurriculum = enrollment.Certificate;
                result.Add(new CurriculumEnrollmentModel
                {
                    Id = enrollment.Id,
                    StudentId = enrollment.StudentId.ToString(),
                    StudentName = enrollment.Certificate?.UserName ?? "",
                    CurriculumId = curriculum.Id,
                    CurriculumTitle = curriculum.Title,
                    CoverImageUrl = curriculum.ImageUrl,
                    Description = curriculum.Description,
                    EnrolledAt = enrollment.EnrolledAt,
                    CompletedAt = enrollment.CompletedAt,
                    ProgressPercentage = enrollment.ProgressPercentage,
                    Status = enrollment.Status.ToString(),
                    CertificateId = certCurriculum?.Id,
                    CertificateUrl = certCurriculum?.CertificateUrl,
                    VerificationCode = certCurriculum?.VerificationCode,
                    CourseEnrollments = courseEnrollments
                });
            }

            return new PageList<CurriculumEnrollmentModel>(
                result,
                enrollments.PageNumber,
                enrollments.PageSize,
                enrollments.TotalCount
            );
        }
    }
}