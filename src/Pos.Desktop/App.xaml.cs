using System.Windows;
using Pos.Desktop.Localization;

namespace Pos.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LanguageManager.SetLanguage(LanguageManager.Arabic);

        base.OnStartup(e);
    }
}