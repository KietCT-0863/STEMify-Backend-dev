using Contracts.Abstractions.Paging;

namespace Emulator.Repository.Models;

/// <summary>
/// Simple paged result without implementing IPageList interface
/// </summary>
public class PagedResult<T>
{
    public PagedResult()
    {
        Items = new List<T>();
    }

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int CurrentPageSize => Items.Count;
    public int CurrentStartIndex => (PageNumber - 1) * PageSize + 1;
    public int CurrentEndIndex => CurrentStartIndex + CurrentPageSize - 1;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}