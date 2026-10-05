using System.Windows;
using Pos.Desktop.ViewModels;

namespace Pos.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
