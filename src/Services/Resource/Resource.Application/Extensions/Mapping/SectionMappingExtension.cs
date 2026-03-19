using Contracts.Abstractions.Paging;
using Shared.Protos.Resource;

namespace Resource.Application.Extensions.Mapping
{
    public static class SectionMappingExtension
    {
        // Map Domain SectionResponse to gRPC SectionResponse
        public static Shared.Protos.Resource.SectionResponse ToGrpcSectionResponse(
            this Resource.Application.Models.Section.SectionResponse model
        )
        {
            var grpcSection = new Shared.Protos.Resource.SectionResponse
            {
                Id = model.Id,
                Description = model.Description ?? string.Empty,
                Title = model.Title,
                Duration = model.Duration,
                Status = model.Status.ToString(),
                LessonId = model.LessonId,
                OrderIndex = model.OrderIndex,
                IsVisibleToStudent = model.IsVisibleToStudent,
            };

            grpcSection.ContentIds.AddRange(model.ContentIds ?? new List<int>());
            grpcSection.QuizIds.AddRange(model.QuizIds ?? new List<int>());

            return grpcSection;
        }

        // Map a page of Domain SectionResponse to gRPC PagedSectionList
        public static PagedSectionList ToGrpcPagedSectionList<T>(
            this IPageList<T> paged,
            Func<T, Shared.Protos.Resource.SectionResponse> mapper
        )
            where T : class
        {
            var response = new PagedSectionList
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
