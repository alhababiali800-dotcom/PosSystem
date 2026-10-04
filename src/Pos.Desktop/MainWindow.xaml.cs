using System.Windows;
using Pos.Desktop.Localization;

namespace Pos.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SwitchLanguage_Click(object sender, RoutedEventArgs e)
    {
        LanguageManager.ToggleLanguage();

        // x:Static يُقرأ مرة واحدة عند تحميل النافذة، لذلك نعيد فتحها
        var oldWindow = System.Windows.Application.Current.MainWindow;
        var newWindow = new MainWindow();

        System.Windows.Application.Current.MainWindow = newWindow;
        newWindow.Show();
        oldWindow?.Close();
    }
}