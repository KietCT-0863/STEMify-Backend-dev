using Contracts.Common.Interfaces.Paging;
using Shared.SeedWork;

namespace Infrastructure.Common.Paging
{
    public record PageRequest : IPageRequest
    {
        private const int MaxPageSize = 50;
        private const int DefaultPageSize = 10;
        private const int DefaultPageNumber = 1;

        private int _pageNumber = DefaultPageNumber;
        private int _pageSize = DefaultPageSize;

        public int PageNumber
        {
            get => _pageNumber;
            init => _pageNumber = value < 1 ? DefaultPageNumber : value;
        }

        public int PageSize
        {
            get => _pageSize;
            init =>
                _pageSize =
                    value > MaxPageSize ? MaxPageSize : (value < 1 ? DefaultPageSize : value);
        }

        public string? Filters { get; init; }
        public string? SortOrder { get; init; }

        // For backward compatibility with PagingRequestParam
        public string? OrderBy
        {
            get => SortOrder;
            init => SortOrder = value;
        }

        public void Deconstruct(
            out int pageNumber,
            out int pageSize,
            out string? filters,
            out string? sortOrder
        )
        {
            pageNumber = PageNumber;
            pageSize = PageSize;
            filters = Filters;
            sortOrder = SortOrder;
        }

        // Static factory methods for easier creation
        public static PageRequest Create(
            int pageNumber = DefaultPageNumber,
            int pageSize = DefaultPageSize,
            string? filters = null,
            string? sortOrder = null
        )
        {
            return new PageRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Filters = filters,
                SortOrder = sortOrder,
            };
        }

        public static PageRequest CreateWithOrderBy(
            int pageNumber = DefaultPageNumber,
            int pageSize = DefaultPageSize,
            string? orderBy = null
        )
        {
            return new PageRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderBy = orderBy,
            };
        }

        // Conversion from PagingParamsBase
        public static PageRequest FromPagingParams(PagingRequestParam pagingParams)
        {
            return new PageRequest
            {
                PageNumber = pagingParams.PageNumber,
                PageSize = pagingParams.PageSize,
                //Filters = pagingParams.Filters,
                SortOrder = pagingParams.SortOrder,
            };
        }
    }

    // Extension methods for easier conversion
    public static class PagingExtensions
    {
        public static PageRequest ToPageRequest(this PagingRequestParam pagingParams)
        {
            return PageRequest.FromPagingParams(pagingParams);
        }

        public static IPageRequest ToPageRequest<T>(this T pagingParams)
            where T : PagingRequestParam
        {
            return PageRequest.FromPagingParams(pagingParams);
        }
    }
}
