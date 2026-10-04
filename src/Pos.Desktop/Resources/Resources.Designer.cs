#nullable enable

using System.Globalization;
using System.Resources;

namespace Pos.Desktop.Resources;

public class Resources
{
    private static ResourceManager? _resourceManager;
    private static CultureInfo? _culture;

    public static ResourceManager ResourceManager =>
        _resourceManager ??= new ResourceManager(
            "Pos.Desktop.Resources.Resources",
            typeof(Resources).Assembly);

    public static CultureInfo? Culture
    {
        get => _culture;
        set => _culture = value;
    }

    public static string AppTitle => ResourceManager.GetString("AppTitle", _culture) ?? "AppTitle";
    public static string Save => ResourceManager.GetString("Save", _culture) ?? "Save";
    public static string Cancel => ResourceManager.GetString("Cancel", _culture) ?? "Cancel";
    public static string Search => ResourceManager.GetString("Search", _culture) ?? "Search";
    public static string Products => ResourceManager.GetString("Products", _culture) ?? "Products";
    public static string Categories => ResourceManager.GetString("Categories", _culture) ?? "Categories";
    public static string SwitchLanguage => ResourceManager.GetString("SwitchLanguage", _culture) ?? "SwitchLanguage";
}