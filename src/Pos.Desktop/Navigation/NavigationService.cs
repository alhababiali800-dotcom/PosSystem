using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Pos.Desktop.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services) => _services = services;

    public ObservableObject? Current { get; private set; }

    public event Action? CurrentChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        Current = _services.GetRequiredService<TViewModel>();
        CurrentChanged?.Invoke();
    }
}