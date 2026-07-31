namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// Backs the shared "trang khung" empty state used by every skeleton page
/// (Doctor or Patient area) that doesn't have a real feature behind it yet.
/// </summary>
public class ComingSoonViewModel
{
    public string Icon { get; set; } = "construction";

    public string Message { get; set; } = "Tính năng đang được phát triển.";

    public string? SubMessage { get; set; }
}
