using AC_Unit.ViewModels;
using Iot_Recources.Data;
using Iot_Recources.Services;
using System.Windows;

namespace AC_Unit.Views;

public partial class MainWindow : Window
{
    private readonly IDatabaseContext _context;
    public MainWindow(MainWindowViewModel viewModel, IDatabaseContext context)
    {
        InitializeComponent();
        DataContext = viewModel;
        _context = context;
    }
}
