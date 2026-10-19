using System.ComponentModel;
using System.Windows.Input;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soenneker.Utils.String.LongestCommonSequence;
using ZLinq;

namespace WpfApp1.Models;

public partial class MainWindowModel : ObservableObject
{
    private string OutputExcelPath { get; }
    private Dictionary<Record, ISet<Record>> Statistic { get; } = new();
    private ISet<Record> CurrentRecordSet { get; } = new HashSet<Record>();

    public MainWindowModel(string inputExcelPath, string outputExcelPath)
    {
        OutputExcelPath = outputExcelPath;
        NextCommand = new RelayCommand(Next);
        SaveCommand = new RelayCommand(Save);
        Workbook = new XLWorkbook(inputExcelPath);
        AllRecords = GetAllRecords();
        Next();
    }

    private void RecordOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var record = (Record)sender!;
        if (e.PropertyName != nameof(CurrentRecord.IsChecked)) return;

        var currentRecord = CurrentRecord!;
        if (!Statistic.ContainsKey(currentRecord)) Statistic[currentRecord] = new HashSet<Record>();

        var mergedRecords = Statistic[currentRecord];
        switch (record.IsChecked)
        {
            case true when !mergedRecords.Contains(record):
                mergedRecords.Add(record);
                return;
            case false when mergedRecords.Contains(record):
                mergedRecords.Remove(record);
                break;
        }
    }

    [ObservableProperty] public partial Record? CurrentRecord { get; set; }

    [ObservableProperty] public partial IEnumerable<Record>? Records { get; set; }

    partial void OnRecordsChanged(IEnumerable<Record>? oldValue, IEnumerable<Record>? newValue)
    {
        if (oldValue != null)
            foreach (var record in oldValue)
                record.PropertyChanged -= RecordOnPropertyChanged;

        if (newValue != null)
            foreach (var record in newValue)
                record.PropertyChanged += RecordOnPropertyChanged;
    }

    partial void OnCurrentRecordChanged(Record? value)
    {
        if (value == null) return;

        CurrentRecordSet.Add(value);
        Records = GetRelatedRecords(value);
    }

    public ICommand SaveCommand { get; }
    public ICommand NextCommand { get; }

    private void Save()
    {
        foreach (var (targetProject, recordsToMerge) in Statistic)
        {
            var baseInvoiceAmount = decimal.Parse(targetProject.InvoiceAmount);
            foreach (var record in recordsToMerge)
            {
                if (decimal.TryParse(record.InvoiceAmount, out var amount))
                {
                    baseInvoiceAmount += amount;
                }
            }

            var targetCell = targetProject.Row.Cell(2);
            targetCell.Value = baseInvoiceAmount;
        }

        var recordsToDelete = Statistic.Values.SelectMany(v => v).OrderByDescending(x => x.Row.RowNumber()).ToList();
        recordsToDelete.ForEach(x => x.Row.Delete());
        Workbook.SaveAs(OutputExcelPath);
        Workbook.Dispose();
    }

    private void Next()
    {
        var start = CurrentRecord == null ? 0 : CurrentRecord.Row.RowNumber();
        var next = AllRecords.Skip(start)
            .FirstOrDefault(x => !x.IsChecked && decimal.TryParse(x.InvoiceAmount, out _));
        if (next == null) return;

        CurrentRecord = next;
    }

    private IReadOnlyList<Record> AllRecords { get; }

    private IReadOnlyList<Record> GetAllRecords()
    {
        const string sourceSheetName = "Sheet1";
        var worksheet = Workbook.Worksheets.First(x => x.Name.Equals(sourceSheetName));
        var usedRange = worksheet.RangeUsed();
        return usedRange == null
            ? []
            : Enumerable.Range(2, usedRange.RowCount() - 1).Select(x => new Record(worksheet.Row(x))).ToList();
    }

    private IReadOnlyList<Record> GetRelatedRecords(Record currentRecord)
    {
        var similarityCalculator = LcsStringUtil.CalculateSimilarityPercentage;
        return
        [
            .. AllRecords.Where(x => !CurrentRecordSet.Contains(x) && !x.IsChecked).Select(x => new
                {
                    Record = x,
                    Similarity = similarityCalculator(GetProjectName(currentRecord.Name) ?? currentRecord.Name,
                        GetProjectName(x.Name) ?? x.Name)
                })
                .Where(x => x.Similarity >= 50).OrderByDescending(x => x.Similarity).Select(x => x.Record)
        ];
    }

    private XLWorkbook Workbook { get; }

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

        var projectIndex = targetContent.IndexOf("项目", StringComparison.Ordinal);
        if (projectIndex < 0)
        {
            projectIndex = targetContent.IndexOf("工程", StringComparison.Ordinal);
        }

        if (projectIndex > 0)
        {
            targetContent = targetContent[..projectIndex];
        }

        targetContent = targetContent.Replace("周边道路", string.Empty);
        return targetContent;
    }
}