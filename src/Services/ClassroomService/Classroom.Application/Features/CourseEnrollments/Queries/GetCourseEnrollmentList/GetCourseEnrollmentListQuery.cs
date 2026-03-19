using Classroom.Application.Models.EnrollmentModels;
using Classroom.Application.Specifications.CourseEnrollments;
using Infrastructure.Abstractions.Paging;
using MediatR;

namespace Classroom.Application.Features.CourseEnrollments.Queries.GetCourseEnrollmentList
{
    public class GetCourseEnrollmentListQuery : IRequest<PageList<CourseEnrollmentModel>>
    {
        public CourseEnrollmentParams CourseEnrollmentParams { get; set; }

        public GetCourseEnrollmentListQuery(CourseEnrollmentParams courseEnrollmentParams)
        {
            CourseEnrollmentParams =
                courseEnrollmentParams ?? throw new ArgumentNullException(nameof(courseEnrollmentParams));
        }
    }
}
