using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts;

public sealed record PaginationRequest(
    [Range(1, int.MaxValue)] int PageNumber = 1,
    [Range(1, 200)] int PageSize = 50,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = null)
{
    public int Skip => (PageNumber - 1) * PageSize;

    public bool IsDescending =>
        string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
        || string.Equals(SortDirection, "descending", StringComparison.OrdinalIgnoreCase);
}

public sealed record PaginationMeta(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PaginationMeta Create(int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (decimal)pageSize);

        return new PaginationMeta(
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1 && totalCount > 0,
            totalPages > 0 && pageNumber < totalPages);
    }
}

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public PaginationMeta ToPaginationMeta()
    {
        return PaginationMeta.Create(PageNumber, PageSize, TotalCount);
    }
}
