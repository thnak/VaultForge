using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Business.Utils.Excel;

public static class CellExtension
{
    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, string value, HSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, DateTime value, HSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, DateOnly value, HSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, double value, HSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    public static void CreateCellWithValue(this IRow currentRow, int cellIndex, int value, HSSFCellStyle style)
    {
        ICell cell = currentRow.CreateCell(cellIndex);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }
}