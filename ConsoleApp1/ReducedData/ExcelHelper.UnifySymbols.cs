using ClosedXML.Excel;

namespace ReducedData;

public static partial class ExcelHelper
{
    private static void UnifySymbols(string inputExcelPath, string outputExcelPath, bool skipHeader)
    {
        using var workbook = new XLWorkbook(inputExcelPath);
        var worksheet = workbook.Worksheets.First(x => x.Name.Equals(Constants.SourceSheetName));
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null || usedRange.RowCount() < (skipHeader ? 2 : 1))
        {
            workbook.SaveAs(outputExcelPath);
            return;
        }

        var startRow = skipHeader ? 2 : 1;
        var totalRowCount = usedRange.RowCount();
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            Console.WriteLine($"Handling row: {rowNum}");
            var currentRow = worksheet.Row(rowNum);
            var currentCell = currentRow.Cell(1);
            var content = currentRow.Cell(1).GetString().Trim();
            content = content.Replace('，', ',');
            content = content.Replace('（', '(');
            content = content.Replace('）', ')');
            content = content.Replace('“', '"');
            content = content.Replace('”', '"');
            content = content.Replace('：', ':');
            content = content.Replace('；', ';');
            currentCell.Value = content;
        }

        workbook.SaveAs(outputExcelPath);
    }
}