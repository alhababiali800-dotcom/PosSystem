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

    // --- المفاتيح القديمة ---
    public static string AppTitle => ResourceManager.GetString("AppTitle", _culture) ?? "AppTitle";
    public static string Save => ResourceManager.GetString("Save", _culture) ?? "Save";
    public static string Cancel => ResourceManager.GetString("Cancel", _culture) ?? "Cancel";
    public static string Search => ResourceManager.GetString("Search", _culture) ?? "Search";
    public static string Products => ResourceManager.GetString("Products", _culture) ?? "Products";
    public static string Categories => ResourceManager.GetString("Categories", _culture) ?? "Categories";
    public static string SwitchLanguage => ResourceManager.GetString("SwitchLanguage", _culture) ?? "SwitchLanguage";

    // --- المفاتيح الجديدة ---
    public static string LoginTitle => ResourceManager.GetString("LoginTitle", _culture) ?? "LoginTitle";
    public static string LoginSubtitle => ResourceManager.GetString("LoginSubtitle", _culture) ?? "LoginSubtitle";
    public static string Username => ResourceManager.GetString("Username", _culture) ?? "Username";
    public static string Password => ResourceManager.GetString("Password", _culture) ?? "Password";
    public static string Login => ResourceManager.GetString("Login", _culture) ?? "Login";
    public static string PointOfSale => ResourceManager.GetString("PointOfSale", _culture) ?? "PointOfSale";
    public static string Logout => ResourceManager.GetString("Logout", _culture) ?? "Logout";
    public static string CurrentUser => ResourceManager.GetString("CurrentUser", _culture) ?? "CurrentUser";
}