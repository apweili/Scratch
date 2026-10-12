using Serilog;
using ClosedXML.Excel;
using ZLinq;

namespace ReducedData;

public static class ExcelDeduplicationV1Helper
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File("logs/app_log.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger().ForContext(typeof(ExcelDeduplicationV1Helper));

    public static void TestRun()
    {
        const string folder = @"D:\workspace\test\Scratch\ConsoleApp1\ReducedData\cwj";
        MergeDuplicatesByFirstCol(@$"{folder}\副本取票汇总表.xlsx",
            @$"{folder}\取票汇总表trimmed.xlsx", true);
    }

    public static void MergeDuplicatesByFirstCol(string inputExcelPath, string outputExcelPath, bool skipHeader)
    {
        using var workbook = new XLWorkbook(inputExcelPath);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();

        if (usedRange == null || usedRange.RowCount() < (skipHeader ? 2 : 1))
        {
            workbook.SaveAs(outputExcelPath);
            return;
        }

        var startRow = skipHeader ? 2 : 1;
        var uniqueRows = new Dictionary<string, IXLRow>();
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
                Console.WriteLine("row content is empty, reserved");
                continue;
            }

            var firstColRawContent = currentRow.Cell(1).GetString().Trim();
            var firstColKey = GetRowKey(firstColRawContent);
            if (string.IsNullOrWhiteSpace(firstColKey))
            {
                Console.WriteLine("row key is empty, reserved");
                continue;
            }

            if (!decimal.TryParse(currentRow.Cell(columnCount).GetString(), out _))
            {
                Console.WriteLine("Invalid invoice amount, reserved");
                uniqueRows.Add(firstColRawContent, currentRow);
                continue;
            }

            if (!uniqueRows.TryGetValue(firstColKey, out var existedRow))
            {
                Console.WriteLine($"Row: {rowNum} reserved");
                uniqueRows.Add(firstColKey, currentRow);
                continue;
            }

            DoSomethingAfterFindingSameProject(ref existedRow, ref currentRow);
            Logger.Information(
                "Merge from:\n {CurrentProjectName}\n to===========================>>>>>>>>>>>>>>>>>>>>>>>>>>>\n {ExistProjectName}\n",
                firstColRawContent,
                existedRow.Cell(1).GetString().Trim());
            rowsToDelete.Add(currentRow);
            Console.WriteLine($"Row: {rowNum} deleted");
        }

        rowsToDelete.Reverse();
        Console.WriteLine($"Total row count: {rowsToDelete.Count} deleted");
        rowsToDelete.ForEach(r => { r.Delete(); });
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

    private static string GetRowKey(string colName)
    {
        // ReSharper disable once RedundantAssignment
        var key = string.Empty;
        // one by one
        // key = GetProjectName1(colName);
        key = GetProjectName2(colName);
        return string.IsNullOrEmpty(key) ? colName : key;
    }

    private static string GetProjectName1(string colName)
    {
        var flagIndex = colName.IndexOf("项目名称", StringComparison.Ordinal);
        if (flagIndex < 0)
        {
            flagIndex = colName.IndexOf("工程名称", StringComparison.Ordinal);
        }

        if (flagIndex < 0)
        {
            flagIndex = colName.IndexOf("合同名称", StringComparison.Ordinal);
        }

        if (flagIndex < 0)
        {
            return colName;
        }

        colName = colName[flagIndex..];
        colName = colName.Replace(":", "：");
        var parameters = colName.Split('：', StringSplitOptions.RemoveEmptyEntries);
        if (parameters.Length <= 1) return colName;
        var firstInput = parameters[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return firstInput[0];
    }

    private static string GetProjectName2(string colName)
    {
        var nameSpan = colName.AsSpan();
        var flagIndex = nameSpan.IndexOf("项目名称", StringComparison.Ordinal);
        if (flagIndex < 0)
        {
            flagIndex = nameSpan.IndexOf("工程名称", StringComparison.Ordinal);
        }

        if (flagIndex < 0)
        {
            flagIndex = nameSpan.IndexOf("合同名称", StringComparison.Ordinal);
        }

        if (flagIndex < 0)
        {
            return colName;
        }
      
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

        if (!foundColon || start >= colName.Length)
        {
            return colName;
        }

        var targetContentSpan = nameSpan[start..];
        char[] splitter = ['\r', '\n', ' '];
        var targetContent =
            new string(targetContentSpan.AsValueEnumerable().SkipWhile(x => splitter.Contains(x))
                .TakeWhile(x => !splitter.Contains(x)).ToArray());
        return targetContent;
    }

    private static string GetProjectName3(string colName)
    {
        var parameters = colName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parameters[0];
    }
}