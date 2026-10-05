using CommunityToolkit.Mvvm.ComponentModel;

namespace Pos.Desktop.Navigation;

public interface INavigationService
{
    ObservableObject? Current { get; }
    event Action? CurrentChanged;
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}