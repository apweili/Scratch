using System.Windows;
using WpfApp1.Models;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainWindowModel(@$"{Constants.RootFolderName}\副本取票汇总表.xlsx",
            @$"{Constants.RootFolderName}\取票汇总表trimmed.xlsx");
        DataContext = vm;
    }
}