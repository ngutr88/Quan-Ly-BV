using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// Adapts a <see cref="PagedList{T}"/> for the shared pagination partial,
/// carrying the current filter values so page links preserve the active search.
/// </summary>
public class PaginationViewModel
{
    public int PageNumber { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalCount { get; set; }

    public int FirstItemIndex { get; set; }

    public int LastItemIndex { get; set; }

    public bool HasPrevious { get; set; }

    public bool HasNext { get; set; }

    /// <summary>Path the page links point at, e.g. "/Admin/Patients".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Plural noun used in the "showing X of Y" summary.</summary>
    public string ItemLabel { get; set; } = "bản ghi";

    /// <summary>Active filters carried across pages (excluding "page").</summary>
    public IDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>();

    public static PaginationViewModel From<T>(
        PagedList<T> page,
        string path,
        string itemLabel,
        IDictionary<string, string>? routeValues = null) => new()
        {
            PageNumber = page.PageNumber,
            TotalPages = page.TotalPages,
            TotalCount = page.TotalCount,
            FirstItemIndex = page.FirstItemIndex,
            LastItemIndex = page.LastItemIndex,
            HasPrevious = page.HasPrevious,
            HasNext = page.HasNext,
            Path = path,
            ItemLabel = itemLabel,
            RouteValues = routeValues ?? new Dictionary<string, string>()
        };
}
