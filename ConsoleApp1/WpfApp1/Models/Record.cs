using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp1.Models;

public partial class Record : ObservableObject
{
    public Record(IXLRow row)
    {
        Row = row;
        Name = Row.Cell(1).GetString().Trim();
        InvoiceAmount = Row.Cell(2).GetString().Trim();
    }

    public IXLRow Row { get; }
    public string Name { get; }
    public string InvoiceAmount { get; }
    [ObservableProperty] public partial bool IsChecked { get; set; }
}