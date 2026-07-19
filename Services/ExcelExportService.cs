using ClosedXML.Excel;

namespace QuanLyBenhVien.Services;

/// <summary>
/// Builds .xlsx workbooks for the admin list screens. Kept generic so each
/// controller only describes its columns rather than repeating workbook setup.
/// </summary>
public class ExcelExportService
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Renders <paramref name="rows"/> into a single-sheet workbook with a
    /// hospital-branded title block, a styled header row and auto-fitted columns.
    /// </summary>
    public byte[] Build<T>(
        string sheetName,
        string title,
        IReadOnlyList<ExcelColumn<T>> columns,
        IReadOnlyList<T> rows,
        string? subtitle = null)
    {
        using var workbook = new XLWorkbook();

        // Excel caps sheet names at 31 chars and forbids several characters.
        var safeSheetName = SanitiseSheetName(sheetName);
        var sheet = workbook.Worksheets.Add(safeSheetName);

        var lastColumn = Math.Max(columns.Count, 1);

        var titleCell = sheet.Cell(1, 1);
        titleCell.Value = title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, lastColumn).Merge();

        var subtitleText = string.IsNullOrWhiteSpace(subtitle)
            ? $"Xuất lúc {DateTime.Now:HH:mm dd/MM/yyyy}"
            : $"{subtitle} • Xuất lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
        var subtitleCell = sheet.Cell(2, 1);
        subtitleCell.Value = subtitleText;
        subtitleCell.Style.Font.FontSize = 10;
        subtitleCell.Style.Font.FontColor = XLColor.FromHtml("#627D98");
        sheet.Range(2, 1, 2, lastColumn).Merge();

        const int headerRow = 4;
        for (var c = 0; c < columns.Count; c++)
        {
            var cell = sheet.Cell(headerRow, c + 1);
            cell.Value = columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B5CAB");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(headerRow + 1 + r, c + 1);
                SetCellValue(cell, columns[c].Value(rows[r]));

                if (!string.IsNullOrEmpty(columns[c].NumberFormat))
                {
                    cell.Style.NumberFormat.Format = columns[c].NumberFormat;
                }
            }
        }

        if (rows.Count > 0)
        {
            sheet.Range(headerRow, 1, headerRow + rows.Count, lastColumn)
                 .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns().AdjustToContents(headerRow, headerRow + rows.Count, 10d, 55d);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case string s:
                cell.Value = s;
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                break;
            case decimal dec:
                cell.Value = dec;
                break;
            case double dbl:
                cell.Value = dbl;
                break;
            case int i:
                cell.Value = i;
                break;
            case long l:
                cell.Value = l;
                break;
            case bool b:
                cell.Value = b ? "Có" : "Không";
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static string SanitiseSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var cleaned = new string(name.Where(ch => !invalid.Contains(ch)).ToArray());
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Sheet1";
        }
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    /// <summary>Builds a timestamped, browser-safe download name.</summary>
    public static string FileName(string prefix) =>
        $"{prefix}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
}

/// <summary>Describes one exported column: its header and how to read it.</summary>
public class ExcelColumn<T>
{
    public ExcelColumn(string header, Func<T, object?> value, string? numberFormat = null)
    {
        Header = header;
        Value = value;
        NumberFormat = numberFormat;
    }

    public string Header { get; }

    public Func<T, object?> Value { get; }

    public string? NumberFormat { get; }
}
