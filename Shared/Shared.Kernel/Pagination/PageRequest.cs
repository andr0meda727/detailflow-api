using System.ComponentModel.DataAnnotations;

namespace Shared.Kernel.Pagination;

/// <summary>
/// Offset paging parameters, bound from <c>?page=&amp;pageSize=</c>. Out-of-range values are rejected
/// by model validation instead of silently clamped, so the client always gets the page it asked for.
/// </summary>
public sealed class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxPage = 100_000;

    [Range(1, MaxPage)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public int Skip => (Page - 1) * PageSize;
}
