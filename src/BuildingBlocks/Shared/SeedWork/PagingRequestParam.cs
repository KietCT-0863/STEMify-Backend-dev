using Shared.Enums;

namespace Shared.SeedWork
{
    /// <summary>
    /// Base class for paging parameters with inheritance support for service-specific parameters
    /// Contains validation logic merged from PagingRequestParam
    /// Services should convert this to PageRequest when calling Infrastructure layer
    /// </summary>
    public abstract class PagingRequestParam
    {
        private const int MaxPageSize = 50;
        private const int DefaultPageSize = 10;
        private const int DefaultPageNumber = 1;

        private int _pageNumber = DefaultPageNumber;
        private int _pageSize = DefaultPageSize;

        public virtual int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? DefaultPageNumber : value;
        }

        public virtual int PageSize
        {
            get => _pageSize;
            set =>
                _pageSize =
                    value > MaxPageSize ? MaxPageSize : (value < 1 ? DefaultPageSize : value);
        }

        public string? OrderBy { get; set; }
        public SortDirection SortDirection { get; set; } = SortDirection.Asc;

        // Optional filters for advanced scenarios
        //public string? Filters { get; set; }
        public string? SortOrder => OrderBy; // Alias for compatibility
    }
}
