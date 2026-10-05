using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Pos.Desktop.Localization;

namespace Pos.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SwitchLanguage_Click(object sender, RoutedEventArgs e)
    {
        LanguageManager.ToggleLanguage();

        var newWindow = App.Services.GetRequiredService<MainWindow>();
        var oldWindow = System.Windows.Application.Current.MainWindow;

        System.Windows.Application.Current.MainWindow = newWindow;
        newWindow.Show();
        oldWindow?.Close();
    }
}
