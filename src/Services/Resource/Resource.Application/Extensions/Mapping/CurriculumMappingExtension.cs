using Contracts.Abstractions.Paging;
using Shared.Protos.Resource;

namespace Resource.Application.Extensions.Mapping
{
    public static class CurriculumMappingExtension
    {
        // Map Domain CurriculumResponse to gRPC CurriculumResponse
        public static CurriculumResponse ToGrpcCurriculumResponse(
            this Models.Curriculum.CurriculumResponse model
        )
        {
            var grpcCurriculum = new CurriculumResponse
            {
                Id = model.Id,
                Code = model.Code,
                Title = model.Title ?? string.Empty,
                ImageUrl = model.ImageUrl ?? string.Empty,
                Description = model.Description ?? string.Empty,
                Status = model.Status.ToString(),
                CreatedByUserId = model.CreatedByUserId ?? string.Empty,
                CreatedByUserName = model.CreatedByUserName ?? string.Empty,
                ApprovedByUserId = model.ApprovedByUserId,
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                    model.CreatedDate
                ),
                CourseCount = model.CourseCount,
            };

            if (model.LastModifiedDate != null)
                grpcCurriculum.LastModifiedDate =
                    Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        model.LastModifiedDate.Value
                    );
            if (model.ApprovedAt != null)
            {
                grpcCurriculum.ApprovedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    model.ApprovedAt.Value
                );
            }

            return grpcCurriculum;
        }

        // Map a page of Domain CurriculumResponse to gRPC PagedCurriculumList
        public static PagedCurriculumList ToGrpcPagedCurriculumList<T>(
            this IPageList<T> paged,
            Func<T, CurriculumResponse> mapper
        )
            where T : class
        {
            var response = new PagedCurriculumList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            response.Items.AddRange(paged.Items.Select(mapper));
            return response;
        }
    }
}
