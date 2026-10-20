using System.ComponentModel;
using System.Windows.Input;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soenneker.Utils.String.LongestCommonSequence;
using WpfApp1.Extensions;

namespace WpfApp1.Models;

public partial class MainWindowModel : ObservableObject
{
    private string OutputExcelPath { get; }
    private Dictionary<Record, ISet<Record>> Statistic { get; } = new();
    private ISet<Record> CurrentRecordSet { get; } = new HashSet<Record>();
    private IDictionary<string, ISet<Record>> RelatedRecordSets { get; } = new Dictionary<string, ISet<Record>>();

    public MainWindowModel(string inputExcelPath, string outputExcelPath)
    {
        OutputExcelPath = outputExcelPath;
        MarkCommand = new RelayCommand(Mark);
        SearchCommand = new RelayCommand(Search);
        NextCommand = new RelayCommand(Next);
        SaveCommand = new RelayCommand(Save);
        Workbook = new XLWorkbook(inputExcelPath);
        AllRecords = GetAllRecords();
        Next();
    }

    private void Mark()
    {
        CurrentRecord?.Mark();
    }

    private void Search()
    {
        var filter = Filter;
        var records = Records;
        if (string.IsNullOrEmpty(filter))
        {
            foreach (var record in records)
            {
                record.IsVisible = true;
            }
            return;
        }

        foreach (var record in records.Where(x => !x.Name.Contains(filter)))
        {
            record.IsVisible = false;
        }
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

    [ObservableProperty] public partial IEnumerable<Record> Records { get; set; }
    [ObservableProperty]
    public partial string Filter { get; set; }

    partial void OnRecordsChanged(IEnumerable<Record>? oldValue, IEnumerable<Record>? newValue)
    {
        if (oldValue != null)
            foreach (var record in oldValue)
            {
                record.PropertyChanged -= RecordOnPropertyChanged;
                if (!record.IsVisible)
                {
                    record.IsVisible = true; 
                }
            }
               

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
    public ICommand SearchCommand { get; }
    public ICommand MarkCommand { get; }
    private bool IsDisposed { get; set; }

    private void Save()
    {
        if (IsDisposed)
        {
            return;
        }
        
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
            
            targetProject.SetInvoiceAmount(baseInvoiceAmount);
        }

        var recordsToDelete = Statistic.Values.SelectMany(v => v).OrderByDescending(x => x.Row.RowNumber()).ToList();
        recordsToDelete.ForEach(x => x.Row.Delete());
        Workbook.SaveAs(OutputExcelPath);
        Workbook.Dispose();
        IsDisposed = true;
    }

    private void Next()
    {
        CurrentRecord?.Mark();
        while (true)
        {
            var start = CurrentRecord == null ? 0 : CurrentRecord.Row.RowNumber();
            var next = AllRecords.Skip(start - SkipCount)
                .FirstOrDefault(x => !x.IsChecked && !x.IsMarked() && decimal.TryParse(x.InvoiceAmount, out _));
            if (next == null) return;

            CurrentRecord = next;
            if (!Records.Any())
            {
                continue;
            }

            foreach (var record in Records)
            {
                if (RelatedRecordSets.TryGetValue(record.Name, out var dic))
                {
                    dic.Add(record);
                }
                else
                {
                    dic = new HashSet<Record>();
                    dic.Add(record);
                    RelatedRecordSets.Add(record.Name, dic);
                }
            }
            
            break;
        }
    }

    private IReadOnlyList<Record> AllRecords { get; }
    private const int SkipCount = 0;

    private IReadOnlyList<Record> GetAllRecords()
    {
        const string sourceSheetName = "Sheet1";
        var worksheet = Workbook.Worksheets.First(x => x.Name.Equals(sourceSheetName));
        var usedRange = worksheet.RangeUsed();
        return usedRange == null
            ? []
            : Enumerable.Range(2, usedRange.RowCount() - 1).Skip(SkipCount).Select(x => new Record(worksheet.Row(x))).ToList();
    }

    private IReadOnlyList<Record> GetRelatedRecords(Record currentRecord)
    {
        var similarityCalculator = LcsStringUtil.CalculateSimilarityPercentage;
        return
        [
            .. AllRecords.Where(x =>
                    !CurrentRecordSet.Contains(x) && !x.IsChecked &&
                    (!RelatedRecordSets.TryGetValue(x.Name, out var dic) || dic.Contains(currentRecord))).Select(x =>
                    new
                    {
                        Record = x,
                        Similarity = similarityCalculator(
                            string.IsNullOrEmpty(currentRecord.Key) ? currentRecord.Name : currentRecord.Key,
                            string.IsNullOrEmpty(x.Key) ? x.Name : x.Key)
                    })
                .Where(x => x.Similarity >= 1).OrderByDescending(x => x.Similarity).Select(x => x.Record)
        ];
    }

    private XLWorkbook Workbook { get; }
}