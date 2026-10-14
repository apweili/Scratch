using ClosedXML.Excel;

namespace ReducedData;

public static partial class ExcelHelper
{
    private static void DeleteWaterFee(string inputExcelPath, string outputExcelPath, bool skipHeader)
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
        Console.WriteLine($"Total row to handle: {totalRowCount}");
        var rowsToDelete = new List<IXLRow>();
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            Console.WriteLine($"Handling row: {rowNum}");
            var currentRow = worksheet.Row(rowNum);
            var firstColRawContent = currentRow.Cell(1).GetString().Trim();
            if (!NeedDelete(firstColRawContent)) continue;

            Logger.Information(
                "delete :\n {CurrentProjectName}\n ", firstColRawContent);
            rowsToDelete.Add(currentRow);
            Console.WriteLine($"Row: {rowNum} deleted");
        }

        rowsToDelete.Reverse();
        rowsToDelete.ForEach(r => { r.Delete(); });
        Console.WriteLine($"Total row count: {rowsToDelete.Count} deleted");
        workbook.SaveAs(outputExcelPath);
        return;

        static bool NeedDelete(string content)
        {
            // if (content.StartsWith("户号：") || content.StartsWith("户号:") ||
            //     content.StartsWith("机械表"))
            // if (content.EndsWith("季度维护费"))
            // {
            //     return true;
            // }
            //
            // if (content.EndsWith("自请人员工资及社保"))
            // {
            //     return true;
            // }
            //
            // if (content.StartsWith("帐期：") || content.StartsWith("帐期:"))
            // {
            //     return true;
            // }
            if (content.Contains("红字发票信息表编号"))
            {
                return true;
            }
            
            return false;
        }
    }
 
}