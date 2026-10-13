using ClosedXML.Excel;
using ZLinq;

namespace ReducedData;

public static partial class ExcelDeduplicationV1Helper
{
    private static void StatisticAllProject(string inputExcelPath, string outputExcelPath, bool skipHeader)
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
        var projectRows = new Dictionary<string, IXLRow>();
        var rowSet = new HashSet<int>();
        const int columnCount = 2;
        var totalRowCount = usedRange.RowCount();
        Console.WriteLine($"Total row to handle: {totalRowCount}");
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            Console.WriteLine($"Handling row: {rowNum}");
            var currentRow = worksheet.Row(rowNum);
            if (currentRow.Cell(1).IsEmpty())
            {
                Console.WriteLine("row content is empty, reserved");
                continue;
            }

            var firstColRawContent = currentRow.Cell(1).GetString().Trim();
            var projectName = GetProjectName(firstColRawContent);
            if (string.IsNullOrEmpty(projectName))
            {
                continue;
            }

            if (projectRows.TryGetValue(projectName, out _)) continue;

            rowSet.Add(rowNum);
            projectRows.Add(projectName, currentRow);
        }
        
        var rowsToDelete = new List<IXLRow>();
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            if (rowSet.Contains(rowNum))
            {
                continue;
            }
            
            Console.WriteLine($"Handling row: {rowNum}");
            var currentRow = worksheet.Row(rowNum);
            if (currentRow.Cell(1).IsEmpty())
            {
                Console.WriteLine("row content is empty, reserved");
                continue;
            }

            if (!decimal.TryParse(currentRow.Cell(columnCount).GetString(), out _))
            {
                Console.WriteLine("Invalid invoice amount, reserved");
                continue;
            }

            var firstColRawContent = currentRow.Cell(1).GetString().Trim();
            var referenceProject = projectRows.Keys.FirstOrDefault(x =>
            {
                var index = firstColRawContent.IndexOf(x, StringComparison.Ordinal);
                return index >= 0; // && index < firstColRawContent.Length / 2;
            });
            if (string.IsNullOrEmpty(referenceProject)) continue;

            var existedRow = projectRows[referenceProject];
            rowsToDelete.Add(currentRow);
            DoSomethingAfterFindingSameProject(ref existedRow, ref currentRow);
            Logger.Information(
                "Merge from:\n {CurrentProjectName}\n to===========================>>>>>>>>>>>>>>>>>>>>>>>>>>>\n {ExistProjectName}\n",
                firstColRawContent,
                existedRow.Cell(1).GetString().Trim());
        }

        rowsToDelete.Reverse();
        rowsToDelete.ForEach(r => { r.Delete(); });
        Console.WriteLine($"Total row count: {rowsToDelete.Count} deleted");
        workbook.SaveAs(outputExcelPath);
        return;

        static void DoSomethingAfterFindingSameProject(ref IXLRow existedRow, ref IXLRow currentRow)
        {
            var existCell = existedRow.Cell(columnCount);
            var existInvoiceAmount = decimal.Parse(existCell.GetString());
            var currentInvoiceAmount = decimal.Parse(currentRow.Cell(columnCount).GetString());
            existCell.Value = $"{existInvoiceAmount + currentInvoiceAmount}";
        }
    }

    private static string GetProjectName(string colName)
    {
        var nameSpan = colName.AsSpan();
        var flagIndex = nameSpan.IndexOf("项目名称", StringComparison.Ordinal);
        if (flagIndex < 0) flagIndex = nameSpan.IndexOf("工程名称", StringComparison.Ordinal);

        if (flagIndex < 0) flagIndex = nameSpan.IndexOf("合同名称", StringComparison.Ordinal);

        if (flagIndex < 0) return string.Empty;

        var start = flagIndex + 4;
        var foundColon = false;
        while (start < colName.Length - 1)
        {
            var currentChar = colName[start];
            start++;
            if (currentChar is not (':' or '：')) continue;

            foundColon = true;
            break;
        }

        if (!foundColon || start >= colName.Length) return string.Empty;

        var targetContentSpan = nameSpan[start..];
        char[] splitter = ['\r', '\n', ' ', ':', '：'];
        var targetContent =
            new string(targetContentSpan.AsValueEnumerable().SkipWhile(x => splitter.Contains(x))
                .TakeWhile(x => !splitter.Contains(x)).ToArray());
        if (targetContent.EndsWith("项目") || targetContent.EndsWith("工程"))
        {
            targetContent = targetContent[..^2];
        }
        else if (decimal.TryParse(targetContent, out _))
        {
            targetContentSpan = nameSpan[(start + targetContent.Length + 1)..];
            targetContent += new string(targetContentSpan.AsValueEnumerable().SkipWhile(x => splitter.Contains(x))
                .TakeWhile(x => !splitter.Contains(x)).ToArray());
        }

        return targetContent;
    }
}