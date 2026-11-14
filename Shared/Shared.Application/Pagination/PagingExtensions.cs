using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Pagination;

namespace Shared.Application.Pagination;

public static class PagingExtensions
{
    /// <summary>
    /// Counts the filtered query and loads one page of it. The query must already be ordered,
    /// ending with a unique key, otherwise rows can repeat or go missing between pages.
    /// </summary>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IOrderedQueryable<T> query, PageRequest page, CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = totalCount > page.Skip
            ? await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken)
            : [];

        return new PagedResponse<T>(items, page.Page, page.PageSize, totalCount);
    }

    /// <summary>
    /// Loads up to <paramref name="limit"/> rows of a query that is already ordered and already filtered to the rows
    /// after the cursor. One extra row is read to tell whether another slice follows; if it does, the next cursor
    /// points after the last row returned.
    /// </summary>
    public static async Task<CursorResponse<T>> ToCursorResponseAsync<T>(
        this IOrderedQueryable<T> query, int limit, Func<T, string> cursorAfter,
        CancellationToken cancellationToken = default)
    {
        var rows = await query.Take(limit + 1).ToListAsync(cancellationToken);
        if (rows.Count <= limit) return new CursorResponse<T>(rows, null);

        rows.RemoveAt(limit);
        return new CursorResponse<T>(rows, cursorAfter(rows[^1]));
    }
}
