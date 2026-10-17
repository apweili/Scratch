using Serilog;
using ClosedXML.Excel;
using ZLinq;
using Soenneker.Utils.String.LongestCommonSequence;

namespace ReducedData;

public static partial class ExcelHelper
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File("../../../Logs/app_log.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger().ForContext(typeof(ExcelHelper));

    public static void TestRun()
    {
        // UnifySymbols(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
        //     @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx", true);
        // Distinct(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
        //     @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx", true);
        // TransferRows(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
        //     @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx");
        // DeleteSomeRows(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
        //     @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx", true);
        // MergeDuplicatesByFirstCol(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
        //     @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx", true);
        StatisticAllProject(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
            @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx", true);
        // DeleteWaterFee(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
        //     @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx", true); 
        // FilterWithSameValue(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
        //     @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx", true); 
    }

    private static void MergeDuplicatesByFirstCol(string inputExcelPath, string outputExcelPath, bool skipHeader)
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
            firstColKey ??= firstColRawContent; 
            if (string.IsNullOrEmpty(firstColKey))
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

            if (!NeedMerge(uniqueRows, firstColKey, out var existedRow))
            {
                Console.WriteLine($"Row: {rowNum} reserved");
                uniqueRows.Add(firstColKey, currentRow);
                continue;
            }

            DoSomethingAfterFindingSameProject(ref existedRow!, ref currentRow);
            Logger.Information(
                "Merge from:\n {CurrentProjectName}\n to===========================>>>>>>>>>>>>>>>>>>>>>>>>>>>\n {ExistProjectName}\n",
                firstColRawContent,
                existedRow.Cell(1).GetString().Trim());
            rowsToDelete.Add(currentRow);
            Console.WriteLine($"Row: {rowNum} deleted");
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

        static bool NeedMerge(Dictionary<string, IXLRow> uniqueRows, string firstColKey, out IXLRow? existedRow)
        {
            if (uniqueRows.Count == 0)
            {
                existedRow = null;
                return false;
            }

            if (uniqueRows.TryGetValue(firstColKey, out existedRow))
            {
                return true;
            }

            //var similarityCalculator = Tools.StringHelper.CalculateSimilarity;
            var similarityCalculator = LcsStringUtil.CalculateSimilarityPercentage;
            var similarityPercentage = uniqueRows
                .Select(x => new
                {
                    Key = x.Key,
                    Weight = similarityCalculator(firstColKey, x.Key),
                    Row = x.Value
                }).OrderByDescending(x => x.Weight)
                .First();
            if (similarityPercentage.Weight < 60) return false;
         
            if (Constants.ProjectKeyIndexes.Where(firstColKey.Contains).Any(special => !similarityPercentage.Key.Contains(special)))
            {
                return false;
            }
           
            existedRow = similarityPercentage.Row;
            return true;
        }
    }

    private static string? GetRowKey(string colName)
    {
        return GetProjectNameWithFlagPrefix(colName);
        // ReSharper disable once RedundantAssignment
        var key = string.Empty;
        // one by one
        key = GetProjectName1(colName);
        //key = GetProjectName2(colName);
        return string.IsNullOrEmpty(key) ? colName : key;
    }

    private static string GetProjectName1(string colName)
    {
        var nameSpan = colName.AsSpan();
        var flagIndex = nameSpan.IndexOf("项目名称", StringComparison.Ordinal);
        if (flagIndex < 0) flagIndex = nameSpan.IndexOf("工程名称", StringComparison.Ordinal);

        if (flagIndex < 0) flagIndex = nameSpan.IndexOf("合同名称", StringComparison.Ordinal);

        if (flagIndex < 0) return GetProjectName2(colName);

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

        if (!foundColon || start >= colName.Length) return GetProjectName2(colName);

        var targetContentSpan = nameSpan[start..];
        char[] splitter = ['\r', '\n', ' ', ':', '：'];
        var targetContent =
            new string(targetContentSpan.AsValueEnumerable().SkipWhile(x => splitter.Contains(x))
                .TakeWhile(x => !splitter.Contains(x)).ToArray());
        if (targetContent.EndsWith("项目") || targetContent.EndsWith("工程"))
        {
            targetContent = targetContent[..^2];
        }
        else if (targetContentSpan.Length > targetContent.Length && (targetContentSpan[targetContent.Length] == ':' ||
                                                                     targetContentSpan[targetContent.Length] == '：'))
        {
            if (targetContent.EndsWith("项目名称") || targetContent.EndsWith("工程名称"))
            {
                targetContent = targetContent[..^4];
            }
        }

        return targetContent;
    }

    private static string GetProjectName2(string colName)
    {
        char[] splitter = ['\r', '\n', ' '];
        var parameters = colName.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        var targetContent = parameters[0];
        if (targetContent.EndsWith("项目") || targetContent.EndsWith("工程"))
        {
            targetContent = targetContent[..^2];
        }
        return targetContent;
    }

    private static void DeleteSomeRows(string inputExcelPath, string outputExcelPath, bool skipHeader)
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
        var rowsToDelete = new List<IXLRow>();
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            var currentRow = worksheet.Row(rowNum);
            var firstColRawContent = currentRow.Cell(1).GetString().Trim();
            if ((firstColRawContent.StartsWith("账期:") || firstColRawContent.StartsWith("账期：")) &&
                firstColRawContent.IndexOf("号码", StringComparison.Ordinal) > 0)
            {
                Logger.Information("Row: {RowNum} to be deleted: {FirstColRawContent}", rowNum, firstColRawContent);
                rowsToDelete.Add(currentRow);
            }
        }

        rowsToDelete.Reverse();
        rowsToDelete.ForEach(r => { r.Delete(); });
        Console.WriteLine($"Total row count: {rowsToDelete.Count} deleted");
        workbook.SaveAs(outputExcelPath);
    }
}