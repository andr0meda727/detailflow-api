using System.ComponentModel.DataAnnotations;

namespace Shared.Kernel.Pagination;

/// <summary>
/// Cursor (keyset) paging parameters, bound from <c>?cursor=&amp;limit=</c>. The first request has no cursor; each
/// next one sends the <see cref="CursorResponse{T}.NextCursor"/> it got. Used for "show more" lists that grow at the
/// top, where offset pages would repeat or skip rows when something new arrives in between.
/// </summary>
public sealed class CursorRequest
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    [MaxLength(200)]
    public string? Cursor { get; init; }

    [Range(1, MaxLimit)]
    public int Limit { get; init; } = DefaultLimit;
}

/// <summary>
/// One slice of a cursor-paged list. <see cref="NextCursor"/> is null on the last slice.
/// </summary>
public sealed record CursorResponse<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public CursorResponse<TOut> Map<TOut>(Func<T, TOut> map) => new(Items.Select(map).ToList(), NextCursor);
}
