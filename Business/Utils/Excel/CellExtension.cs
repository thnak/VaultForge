using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Business.Utils.Excel;

public static class CellExtension
{
    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, string value, XSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, DateTime value, XSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, DateOnly value, XSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, double value, XSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, int value, XSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }
}