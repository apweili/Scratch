using System.ComponentModel;
using System.Windows.Input;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        Workbook  = new XLWorkbook(inputExcelPath);
        AllRecords = GetAllRecords(); 
        CurrentRecord = AllRecords.FirstOrDefault();
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
                return;
            case false when mergedRecords.Contains(record):
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
        Workbook.SaveAs(OutputExcelPath);
    }

    private void Next()
    {
        if (CurrentRecord == null) return;

        var next = AllRecords.Skip(CurrentRecord.Row.RowNumber()).FirstOrDefault(x => !x.IsChecked);
        if (next == null) return;

        CurrentRecord = next;
    }

    private IReadOnlyList<Record> AllRecords { get; set; }

    private IReadOnlyList<Record> GetAllRecords()
    {
        const string sourceSheetName = "Sheet1";
        var worksheet = Workbook.Worksheets.First(x => x.Name.Equals(sourceSheetName));
        var usedRange = worksheet.RangeUsed();
        return usedRange == null
            ? []
            : Enumerable.Range(2, usedRange.RowCount() - 1).Select(x => new Record(worksheet.Row(x))).ToList();
    } 

    private static IReadOnlyList<Record> GetRelatedRecords(Record currentRecord)
    {
        return null;
    }

    private XLWorkbook Workbook { get; }
}