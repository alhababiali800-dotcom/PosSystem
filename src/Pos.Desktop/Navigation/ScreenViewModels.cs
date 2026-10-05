using CommunityToolkit.Mvvm.ComponentModel;

namespace Pos.Desktop.ViewModels;

public abstract class ScreenViewModel : ObservableObject
{
    protected ScreenViewModel(string title) => Title = title;

    public string Title { get; }
}

public class PosViewModel : ScreenViewModel
{
    public PosViewModel() : base("POS") { }
}

public class CategoriesViewModel : ScreenViewModel
{
    public CategoriesViewModel() : base("Categories") { }
}

public class ProductsViewModel : ScreenViewModel
{
    public ProductsViewModel() : base("Products") { }
}