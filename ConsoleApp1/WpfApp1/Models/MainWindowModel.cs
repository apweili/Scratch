using System.ComponentModel;
using System.Windows.Input;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soenneker.Utils.String.LongestCommonSequence;

namespace WpfApp1.Models;

public partial class MainWindowModel : ObservableObject
{
    private string OutputExcelPath { get; }
    private Dictionary<Record, ISet<Record>> Statistic { get; } = new();

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
        if (value != null) Records = GetRelatedRecords(value);
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

            targetProject.Row.Value = baseInvoiceAmount;
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
            .. AllRecords.Where(x => x != currentRecord && !x.IsChecked).Select(x => new
                    { Record = x, Similarity = similarityCalculator(currentRecord.Name, x.Name) })
                .Where(x => x.Similarity >= 50).OrderByDescending(x => x.Similarity).Select(x => x.Record)
        ];
    }

    private XLWorkbook Workbook { get; }
}