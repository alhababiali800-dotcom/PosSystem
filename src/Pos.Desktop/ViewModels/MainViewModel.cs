using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Context;
using Pos.Desktop.Localization;
using Pos.Desktop.Navigation;

namespace Pos.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _currentUserName = string.Empty;

    public MainViewModel(INavigationService navigation, IServiceProvider services)
    {
        _navigation = navigation;
        _services = services;

        CurrentUserName = "admin"; // TODO: use SessionManager.Username once DeepSeek adds it

        _navigation.CurrentChanged += OnNavigationChanged;
        _navigation.NavigateTo<PosViewModel>();
    }

    private void OnNavigationChanged() => CurrentViewModel = _navigation.Current;

    [RelayCommand]
    private void NavigateToPos() => _navigation.NavigateTo<PosViewModel>();

    [RelayCommand]
    private void NavigateToCategories() => _navigation.NavigateTo<CategoriesViewModel>();

    [RelayCommand]
    private void NavigateToProducts() => _navigation.NavigateTo<ProductsViewModel>();

    [RelayCommand]
    private void Logout()
    {
        SessionManager.Logout();
        ReplaceShellWith<LoginWindow>();
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        LanguageManager.ToggleLanguage();
        ReplaceShellWith<MainWindow>();
    }

    private void ReplaceShellWith<TWindow>() where TWindow : Window
    {
        try
        {
            var app = System.Windows.Application.Current;

            // Capture the open shell windows BEFORE creating the new one
            var oldShells = app.Windows.OfType<MainWindow>().ToList();

            var newWindow = _services.GetRequiredService<TWindow>();

            _navigation.CurrentChanged -= OnNavigationChanged;

            app.MainWindow = newWindow;
            newWindow.Show();

            foreach (var shell in oldShells)
                shell.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Shell error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
