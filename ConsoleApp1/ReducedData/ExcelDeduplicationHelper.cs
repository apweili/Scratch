using Serilog;
using ClosedXML.Excel;

namespace ReducedData;

public static class ExcelDeduplicationV1Helper
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File("logs/app_log.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger().ForContext(typeof(ExcelDeduplicationV1Helper));

    // 直接可运行的调用示例
    public static void TestRun()
    {
        const string folder = @"D:\workspace\test\Scratch\ConsoleApp1\ReducedData\cwj";
        // 直接替换为你的工控数据文件路径即可
        MergeDuplicatesByFirstCol(@$"{folder}\副本取票汇总表.xlsx",
            @$"{folder}\取票汇总表trimmed.xlsx", true);
    }

    /// <summary>
    /// 完全适配 ClosedXML.Excel 1.0.0 版本 按第一列合并重复行
    /// </summary>
    /// <param name="inputExcelPath">原始Excel路径</param>
    /// <param name="outputExcelPath">处理后输出路径</param>
    /// <param name="skipHeader">是否保留首行表头，默认true</param>
    public static void MergeDuplicatesByFirstCol(string inputExcelPath, string outputExcelPath, bool skipHeader)
    {
        // 1.0.0 版本原生支持直接加载文件，无参数重载兼容
        using var workbook = new XLWorkbook(inputExcelPath);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();

        // 兼容1.0.0版本RowCount空判断逻辑
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
        // 1.0.0版本使用显式的ToString()避免隐式值类型转换异常
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            Console.WriteLine($"Handling row: {rowNum}");
            var currentRow = worksheet.Row(rowNum);
            // 修复1.0.0版本IBlank接口判断的兼容问题
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
            Logger.Information("Merge from:\n {CurrentProjectName}\n to=========>\n {ExistProjectName}\n",
                firstColRawContent,
                existedRow.Cell(1).GetString().Trim());
            // 1.0.0版本Delete行后自动重排索引，不会出现后续行号错位问题
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
        // one by one
        // var key = GetProjectName(colName, Constants.ProjectSuffix);
        // var key = GetProjectName(colName, Constants.ProjectSuffix2);
        var key = string.Empty;
        return string.IsNullOrEmpty(key) ? colName : key;
    }

    private static string GetProjectName(string colName, string projectSuffix)
    {
        var parameters = colName.Split([":", "："], StringSplitOptions.RemoveEmptyEntries);
        var targetName = string.Empty;
        if (parameters.Length > 1)
        {
            var startIndex = parameters[0].IndexOf(projectSuffix, StringComparison.Ordinal);
            if (startIndex > 0 && !parameters[0].EndsWith("地点"))
            {
                targetName = parameters[1].Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }
        else
        {
            parameters = colName.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            targetName = IsValid(parameters[0]) ? parameters[0] :
                parameters.Length > 1 ? parameters[1] : string.Empty;
        }

        if (targetName.EndsWith(projectSuffix))
        {
            targetName = targetName[..^projectSuffix.Length];
        }

        return targetName;

        static bool IsValid(string candidate)
        {
            if (candidate.EndsWith("东西湖区"))
            {
                return false;
            }

            if (candidate.StartsWith("合同"))
            {
                return false;
            }

            if (candidate.StartsWith("1、"))
            {
                return false;
            }

            if (candidate.StartsWith("H-2019-造-689"))
            {
                return false;
            }

            if (candidate.StartsWith("H-2019-造-667"))
            {
                return false;
            }

            return true;
        }
    }
}