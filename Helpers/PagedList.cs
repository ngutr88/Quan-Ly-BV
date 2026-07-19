using Microsoft.EntityFrameworkCore;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// A single page of records plus the paging metadata the shared pagination
/// partial needs. Counting and slicing happen in the database so list screens
/// stay fast as the hospital's record volume grows.
/// </summary>
public class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageSize = pageSize < 1 ? DefaultPageSize : pageSize;
        TotalPages = TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        PageNumber = Math.Clamp(pageNumber, 1, TotalPages);
    }

    public const int DefaultPageSize = 15;

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalPages { get; }

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    /// <summary>1-based index of the first record on this page (0 when empty).</summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);

    public int Count => Items.Count;

    /// <summary>
    /// Normalises a requested page size to a supported value so a crafted query
    /// string cannot ask the database for an unbounded page.
    /// </summary>
    public static int NormalisePageSize(int? requested)
    {
        var size = requested ?? DefaultPageSize;
        return size switch
        {
            <= 0 => DefaultPageSize,
            > 100 => 100,
            _ => size
        };
    }
}

public static class PagedListExtensions
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source, int pageNumber, int pageSize)
    {
        pageSize = PagedList<T>.NormalisePageSize(pageSize);
        var totalCount = await source.CountAsync();

        // Clamp before skipping so an out-of-range page shows the last page
        // rather than an empty table.
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var safePage = Math.Clamp(pageNumber < 1 ? 1 : pageNumber, 1, totalPages);

        var items = await source
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<T>(items, totalCount, safePage, pageSize);
    }

    /// <summary>In-memory overload for lists already materialised by a controller.</summary>
    public static PagedList<T> ToPagedList<T>(this IReadOnlyList<T> source, int pageNumber, int pageSize)
    {
        pageSize = PagedList<T>.NormalisePageSize(pageSize);
        var totalPages = source.Count == 0 ? 1 : (int)Math.Ceiling(source.Count / (double)pageSize);
        var safePage = Math.Clamp(pageNumber < 1 ? 1 : pageNumber, 1, totalPages);

        var items = source
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<T>(items, source.Count, safePage, pageSize);
    }
}
