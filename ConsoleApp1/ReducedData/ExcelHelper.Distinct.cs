using ClosedXML.Excel;

namespace ReducedData;

public static partial class ExcelHelper
{
    private static void Distinct(string inputExcelPath, string outputExcelPath, bool skipHeader)
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
        var uniqueRows = new Dictionary<string, decimal?>();
        const int columnCount = 2;
        var totalRowCount = usedRange.RowCount();
        Console.WriteLine($"Total row to handle: {totalRowCount}");
        var rowsToDelete = new List<IXLRow>();
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            Console.WriteLine($"Handling row: {rowNum}");
            var currentRow = worksheet.Row(rowNum);
            if (currentRow.Cell(1).IsEmpty())
            {
                rowsToDelete.Add(currentRow);
                continue;
            }

            var firstColRawContent = currentRow.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(firstColRawContent))
            {
                rowsToDelete.Add(currentRow);
                continue;
            }

            var currentValueStr = currentRow.Cell(columnCount).GetString();
            decimal? nullableCurrentValue = null;
            if (decimal.TryParse(currentValueStr, out var currentValue))
            {
                nullableCurrentValue = currentValue;
            }

            if (!uniqueRows.TryGetValue(firstColRawContent, out var existedValue))
            {
                uniqueRows.Add(firstColRawContent, nullableCurrentValue);
                continue;
            }

            if (nullableCurrentValue == existedValue)
            {
                rowsToDelete.Add(currentRow);
                Logger.Information("Content: {Content} deleted", firstColRawContent);
            }
        }

        rowsToDelete.Reverse();
        rowsToDelete.ForEach(r => { r.Delete(); });
        Console.WriteLine($"Total row count: {rowsToDelete.Count} deleted");
        workbook.SaveAs(outputExcelPath);
    }
}