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
}
