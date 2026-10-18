using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp1.Models;

public partial class Record(IXLRow row) : ObservableObject
{
    public IXLRow Row { get; } = row;

    public string Name => Row.Cell(1).GetString().Trim();
    public string InvoiceAmount => Row.Cell(1).GetString().Trim();
    [ObservableProperty] public partial bool IsChecked { get; set; }
}