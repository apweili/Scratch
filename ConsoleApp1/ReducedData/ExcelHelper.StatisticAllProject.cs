using ClosedXML.Excel;
using Soenneker.Utils.String.LongestCommonSequence;
using ZLinq;

namespace ReducedData;

public static partial class ExcelHelper
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
            var projectName = GetProjectNameWithFlagPrefix(firstColRawContent);
            if (string.IsNullOrEmpty(projectName))
            {
                continue;
            }

            rowSet.Add(rowNum);
            if (projectRows.Count == 0)
            {
                projectRows.Add(projectName, currentRow);
                continue;
            }

            var similarityCalculator = LcsStringUtil.CalculateSimilarityPercentage;
            var similarityPercentage = projectRows
                .Select(x => new
                {
                    Key = x.Key,
                    Weight = similarityCalculator(projectName, x.Key),
                    Row = x.Value
                }).OrderByDescending(x => x.Weight)
                .First();
            var a = Constants.ProjectKeyIndexes.FirstOrDefault(projectName.Contains);
            var isSame = string.IsNullOrEmpty(a) || similarityPercentage.Key.Contains(a);
            if (similarityPercentage.Weight > 50 && isSame)
            {
                var existedRow = similarityPercentage.Row;
                rowsToDelete.Add(currentRow);
                DoSomethingAfterFindingSameProject(ref existedRow, ref currentRow);
                Logger.Information(
                    "Merge from:\n {CurrentProjectName}\n to===========================>>>>>>>>>>>>>>>>>>>>>>>>>>>\n {ExistProjectName}\n",
                    firstColRawContent,
                    existedRow.Cell(1).GetString().Trim());
            }
            else
            {
                projectRows.Add(projectName, currentRow);
            }
        }
        
       
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
                if(index < 0) return false;

                var leftBraceIndex = firstColRawContent.IndexOf('（');
                if(leftBraceIndex < 0)
                {
                    leftBraceIndex = firstColRawContent.IndexOf('(');
                }

                if (leftBraceIndex < 0)
                {
                    return true;
                }

                if (index < leftBraceIndex)
                {
                    return true;
                }

                var rightBraceIndex = firstColRawContent.IndexOf('）');
                if (rightBraceIndex < 0)
                {
                    rightBraceIndex = firstColRawContent.IndexOf(')');
                }

                return index > rightBraceIndex;
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

    private static string? GetProjectNameWithFlagPrefix(string colName)
    {
        var nameSpan = colName.AsSpan();
        var flagIndex = nameSpan.IndexOf("项目名称", StringComparison.Ordinal);
        if (flagIndex < 0) flagIndex = nameSpan.IndexOf("工程名称", StringComparison.Ordinal);

        if (flagIndex < 0) flagIndex = nameSpan.IndexOf("合同名称", StringComparison.Ordinal);

        if (flagIndex < 0) return null;

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

        if (!foundColon || start >= colName.Length) return null;

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
            start = start + targetContent.Length + 1;
            targetContentSpan = nameSpan[start..];
            targetContent += new string(targetContentSpan.AsValueEnumerable().SkipWhile(x => splitter.Contains(x))
                .TakeWhile(x => !splitter.Contains(x)).ToArray());
        }

        start = start + targetContent.Length + 1;
        if (start >= colName.Length - 1)
        {
            return targetContent; 
        }
        
        targetContentSpan = nameSpan[start..];
        var nextTargetContent = new string(targetContentSpan.AsValueEnumerable().SkipWhile(x => splitter.Contains(x))
            .TakeWhile(x => !splitter.Contains(x)).ToArray());
        if (nextTargetContent.StartsWith('，') || nextTargetContent.StartsWith('、'))
        {
            targetContent += nextTargetContent;
        }

        // if (targetContent.EndsWith("路"))
        // {
        //     return null;
        // }

        var projectIndex = targetContent.IndexOf("项目",  StringComparison.Ordinal);
        if (projectIndex < 0)
        {
            projectIndex = targetContent.IndexOf("工程",  StringComparison.Ordinal);
        }
        
        if (projectIndex > 0)
        {
            targetContent = targetContent[..projectIndex]; 
        }
        targetContent = targetContent.Replace("周边道路", string.Empty);
        return targetContent;
    }
}