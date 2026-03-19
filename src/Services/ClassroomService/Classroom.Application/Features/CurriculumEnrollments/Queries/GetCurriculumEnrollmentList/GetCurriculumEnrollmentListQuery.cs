using Classroom.Application.Models.EnrollmentModels;
using Classroom.Application.Specifications.CurriculumEnrollments;
using Infrastructure.Abstractions.Paging;
using MediatR;

namespace Classroom.Application.Features.CurriculumEnrollments.Queries.GetCurriculumEnrollmentList
{
    public class GetCurriculumEnrollmentListQuery : IRequest<PageList<CurriculumEnrollmentModel>>
    {
        public CurriculumEnrollmentParams CurriculumEnrollmentParams { get; set; }

        public GetCurriculumEnrollmentListQuery(CurriculumEnrollmentParams curriculumEnrollmentParams)
        {
            CurriculumEnrollmentParams =
                curriculumEnrollmentParams ?? throw new ArgumentNullException(nameof(curriculumEnrollmentParams));
        }
    }
}
