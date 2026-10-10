using Serilog;
using ClosedXML.Excel;

namespace ReducedData;

public static class ExcelDeduplicationV1Helper
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Information() // 最低记录等级，信息及以上都写入
        .WriteTo.File("logs/app_log.txt",
            rollingInterval: RollingInterval.Day, // 按天自动生成新日志文件，避免单个文件过大
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger().ForContext(typeof(ExcelDeduplicationV1Helper));

    // 直接可运行的调用示例
    public static void TestRun()
    {
        // 直接替换为你的工控数据文件路径即可
        MergeDuplicatesByFirstCol(@"D:\downloads\cwj\副本取票汇总表.xlsx", @"D:\downloads\cwj\取票汇总表trimmed.xlsx", true);
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

            if (!decimal.TryParse(currentRow.Cell(columnCount).GetString(), out var currentInvoiceAmount))
            {
                Console.WriteLine("Invalid invoice amount, reserved");
                uniqueRows.Add(firstColRawContent, currentRow);
                continue;
            }

            if (!uniqueRows.TryGetValue(firstColKey, out var existRow))
            {
                Console.WriteLine($"Row: {rowNum} reserved");
                uniqueRows.Add(firstColKey, currentRow);
                continue;
            }

            var existCell = existRow.Cell(columnCount);
            var existInvoiceAmount = decimal.Parse(existCell.GetString());
            existCell.Value = $"{existInvoiceAmount + currentInvoiceAmount}";
            Logger.Information("Merge from {CurrentProjectName} to {ExistProjectName}", firstColRawContent,
                existRow.Cell(1).GetString().Trim());
            // 1.0.0版本Delete行后自动重排索引，不会出现后续行号错位问题
            rowsToDelete.Add(currentRow);
            Console.WriteLine($"Row: {rowNum} deleted");
        }

        rowsToDelete.Reverse();
        rowsToDelete.ForEach(r => { r.Delete(); });

        workbook.SaveAs(outputExcelPath);
    }

    private static string GetRowKey(string colName)
    {
        var key = GetProjectName(colName);
        return string.IsNullOrEmpty(key) ? colName : key;
    }

    private static string GetProjectName(string colName)
    {
        var parameters = colName.Split([":", "\n", "\r", " "], StringSplitOptions.RemoveEmptyEntries);
        //todo 
        return string.Empty;
    }
}