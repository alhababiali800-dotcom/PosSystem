using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace Pos.Desktop.Localization;

public static class LanguageManager
{
    private static bool _wpfLanguageInitialized;

    public const string English = "en";
    public const string Arabic = "ar";

    public static CultureInfo CurrentCulture { get; private set; } = new(English);

    public static bool IsRtl => CurrentCulture.TextInfo.IsRightToLeft;

    public static FlowDirection CurrentFlowDirection =>
        IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public static event EventHandler? LanguageChanged;

    public static void SetLanguage(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        CurrentCulture = culture;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Pos.Desktop.Resources.Resources.Culture = culture;

        InitializeWpfLanguage(culture);

        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ToggleLanguage() =>
        SetLanguage(CurrentCulture.TwoLetterISOLanguageName == Arabic ? English : Arabic);

    private static void InitializeWpfLanguage(CultureInfo culture)
    {
        if (_wpfLanguageInitialized) return;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        _wpfLanguageInitialized = true;
    }
}