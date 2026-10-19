using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using ZLinq;

namespace WpfApp1.Models;

public partial class Record : ObservableObject
{
    public Record(IXLRow row)
    {
        Row = row;
        Name = Row.Cell(1).GetString().Trim();
        Key = GetProjectName(Name) ?? string.Empty;
        InvoiceAmount = Row.Cell(2).GetString().Trim();
    }

    public string Key { get; }
    public int RowIndex => Row.RowNumber();

    public IXLRow Row { get; }
    public string Name { get; }
    public string InvoiceAmount { get; }
    [ObservableProperty] public partial bool IsChecked { get; set; }

    private static string? GetProjectName(string colName)
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
        if (start >= colName.Length - 1) return targetContent;

        targetContentSpan = nameSpan[start..];
        var nextTargetContent = new string(targetContentSpan.AsValueEnumerable().SkipWhile(x => splitter.Contains(x))
            .TakeWhile(x => !splitter.Contains(x)).ToArray());
        if (nextTargetContent.StartsWith('，') || nextTargetContent.StartsWith('、')) targetContent += nextTargetContent;

        var projectIndex = targetContent.IndexOf("项目", StringComparison.Ordinal);
        if (projectIndex < 0) projectIndex = targetContent.IndexOf("工程", StringComparison.Ordinal);

        if (projectIndex > 0) targetContent = targetContent[..projectIndex];

        targetContent = targetContent.Replace("周边道路", string.Empty);
        return targetContent;
    }
}