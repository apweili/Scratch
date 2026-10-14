using ClosedXML.Excel;

namespace ReducedData;

public static partial class ExcelHelper
{
    private static void FilterWithSameValue(string inputExcelPath, string outputExcelPath, bool skipHeader)
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
        var rows = new List<(decimal, string)>();
        for (var rowNum = startRow; rowNum <= totalRowCount; rowNum++)
        {
            Console.WriteLine($"Handling row: {rowNum}");
            var currentRow = worksheet.Row(rowNum);
            var invoiceAmount = currentRow.Cell(2).GetString().Trim();
            if (decimal.TryParse(invoiceAmount, out var amount))
            {
                var content = currentRow.Cell(1).GetString().Trim();
                rows.Add((amount, content));
            }
        }
        
        foreach (var grouped in rows.GroupBy(r => r.Item1))
        {
            if (grouped.Count() == 1)
            {
                continue;
            }
            
            Logger.Information("===========================>");
            foreach (var valueTuple in grouped)
                Logger.Information("Content: {ProjectName},  InvoiceAmount: {InvoiceAmount}", valueTuple.Item2,
                    valueTuple.Item1);
        }
    }
}