using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Interfaces;
using Pos.Application.UseCases;

namespace Pos.Desktop;

public partial class LoginWindow : Window
{
    private readonly IAuthService _authService;

    public LoginWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (username.Length == 0 || password.Length == 0)
        {
            MessageBox.Show(this, "Please enter both username and password.",
                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoginButton.IsEnabled = false;
        try
        {
            var success = await _authService.LoginAsync(username, password);

            if (!success)
            {
                MessageBox.Show(this, "Invalid username or password.",
                    "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            var mainWindow = App.Services.GetRequiredService<MainWindow>();

            System.Windows.Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Login error: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}