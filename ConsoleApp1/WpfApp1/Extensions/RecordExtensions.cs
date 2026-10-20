using WpfApp1.Models;

namespace WpfApp1.Extensions;

public static class RecordExtensions
{
    private const int Index = 5;
    extension(Record record)
    {
        public bool IsMarked()
        {
            if (record.Row.Cell(Index).IsEmpty())
            {
                return false;
            }
            
            return record.Row.Cell(Index).GetValue<int>() == 1;
        }

        public void Mark()
        {
            record.Row.Cell(Index).SetValue(1);
        }

        public void SetInvoiceAmount(decimal invoiceAmount)
        {
            var targetCell = record.Row.Cell(2);
            targetCell.Value = invoiceAmount;
        }
    }
}