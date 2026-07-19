namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// Drives the shared page header partial so every portal screen opens with the
/// same breadcrumb / title / action layout used across the hospital system.
/// </summary>
public class PageHeaderViewModel
{
    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    /// <summary>Trail shown above the title, ordered from root to parent. The
    /// current page is appended automatically from <see cref="Title"/>.</summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; set; } = Array.Empty<BreadcrumbItem>();

    /// <summary>Label shown for the current page in the breadcrumb when it
    /// should differ from the page title.</summary>
    public string? CurrentCrumb { get; set; }
}

public class BreadcrumbItem
{
    public BreadcrumbItem(string label, string? url = null)
    {
        Label = label;
        Url = url;
    }

    public string Label { get; }

    public string? Url { get; }
}
