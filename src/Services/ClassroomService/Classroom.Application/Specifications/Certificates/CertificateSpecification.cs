using Ardalis.Specification;
using Shared.Enums;

namespace Classroom.Application.Specifications.Certificates
{
    public class CertificateSpecification : Specification<Domain.Entities.Certificate>
    {
        public CertificateSpecification(CertificateParams param)
        {
            if (!string.IsNullOrEmpty(param.Search))
            {
                Query.Where(c =>
                    c.VerificationCode.ToLower().Contains(param.Search)
                );
            }

            if (param.Type.HasValue)
            {
                Query.Where(c => c.CertificateType == param.Type.Value);
            }

            if (!string.IsNullOrEmpty(param.UserId))
            {
                Query.Where(c => c.UserId.ToString() == param.UserId);
            }

            if (param.CourseEnrollmentId.HasValue)
            {
                Query.Where(c => c.CourseEnrollmentId == param.CourseEnrollmentId.Value);
            }

            if (param.CurriculumEnrollmentId.HasValue)
            {
                Query.Where(c => c.CurriculumEnrollmentId == param.CurriculumEnrollmentId.Value);
            }



            // Paging
            Query.Skip((param.PageNumber - 1) * param.PageSize)
                 .Take(param.PageSize);

            // Sorting
            if (!string.IsNullOrEmpty(param.OrderBy))
            {
                // Example: support sorting by IssueDate, add more as needed
                if (param.OrderBy.Equals("IssueDate", StringComparison.OrdinalIgnoreCase))
                {
                    if (param.SortDirection == SortDirection.Desc)
                        Query.OrderByDescending(c => c.IssueDate);
                    else
                        Query.OrderBy(c => c.IssueDate);
                }
            }
        }
    }

    public class CertificateByIdSpecification : Specification<Domain.Entities.Certificate>
    {
        public CertificateByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id);
        }
    }
}
