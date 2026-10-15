using ClosedXML.Excel;

namespace ReducedData;

public static partial class ExcelHelper
{
    private static void TransferRows(string inputExcelPath, string outputExcelPath)
    {
        const string destinationWorksheetName = "项目";
        using var workbook = new XLWorkbook(inputExcelPath);
        var sourceWorksheet = workbook.Worksheets.First();
        var usedRange = sourceWorksheet.RangeUsed();
        if (usedRange == null || usedRange.RowCount() < 1)
        {
            workbook.SaveAs(outputExcelPath);
            return;
        }

        var destinationWorksheet = workbook.Worksheets.FirstOrDefault(x => x.Name == destinationWorksheetName) ??
                                   workbook.AddWorksheet(destinationWorksheetName);
        destinationWorksheet.Unprotect();
        var totalRowCount = usedRange.RowCount();
        var rowsToTransfer = new List<IXLRow>();
        for (var rowNum = 1; rowNum <= totalRowCount; rowNum++)
        {
            var currentRow = sourceWorksheet.Row(rowNum);
            var firstColRawContent = currentRow.Cell(1).GetString().Trim();
            if (NeedTransfer(firstColRawContent))
            {
                Logger.Information("Row: {RowNum} to be transferred: {FirstColRawContent}", rowNum, firstColRawContent);
                rowsToTransfer.Add(currentRow);
            }
        }

        var dataToTransfer = rowsToTransfer.Select(d => new object[] { d.Cell(1).GetString(), d.Cell(2).GetString() })
            .ToList();
        var lastUsedRange = destinationWorksheet.RangeUsed();
        var usedRowCount = lastUsedRange?.RowCount() ?? 0;
        usedRowCount += 1;
        destinationWorksheet.Cell(usedRowCount, 1).InsertData(dataToTransfer);
        rowsToTransfer.Reverse();
        rowsToTransfer.ForEach(r => { r.Delete(); });
        Console.WriteLine($"Total row count: {rowsToTransfer.Count} transferred");
        workbook.SaveAs(outputExcelPath);
        return;

        bool NeedTransfer(string content)
        {
            return content.Contains("项目") || content.Contains("工程");
            return content.Contains("开具红字增值税专用发票信息表编号") || content.Contains("红字发票信息确认单编号");
            return content.Contains("红字发票信息表编号");
            return content.StartsWith("对应正数发票代码:") || content.StartsWith("对应正数发票代码：");
        }
    }
}