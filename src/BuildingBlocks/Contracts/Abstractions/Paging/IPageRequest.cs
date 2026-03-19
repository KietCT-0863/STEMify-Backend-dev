namespace Contracts.Common.Interfaces.Paging
{
    public interface IPageRequest
    {
        int PageNumber { get; init; }
        int PageSize { get; init; }
        string? Filters { get; init; }
        string? SortOrder { get; init; }
        public void Deconstruct(
            out int pageNumber,
            out int pageSize,
            out string? filters,
            out string? sortOrder
        ) =>
            (pageNumber, pageSize, filters, sortOrder) = (PageNumber, PageSize, Filters, SortOrder);
    }
}
