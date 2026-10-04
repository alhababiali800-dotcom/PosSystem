using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.UseCases;
using Pos.Desktop.Localization;
using Pos.Domain.Entities;
using Pos.Infrastructure.Data;

namespace Pos.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await SmokeTestAsync(); // مؤقت: احذفه بعد الاختبار
    }

    private async Task SmokeTestAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<PosDbContext>();
            var categoryService = sp.GetRequiredService<ICategoryService>();
            var productService = sp.GetRequiredService<IProductService>();

            // Product يحتاج UnitId، فننشئ وحدة تجريبية
            var unit = new Pos.Domain.Entities.Unit { Name = "قطعة" };
            db.Units.Add(unit);
            await db.SaveChangesAsync();

            var name = "Test-" + DateTime.Now.ToString("HHmmss");
            var category = await categoryService.AddCategoryAsync(new Category { Name = name });
            await productService.AddProductAsync(new Product
            {
                Name = "Product 1",
                CategoryId = category.Id,
                UnitId = unit.Id
            });

            var products = await productService.GetProductsByCategoryAsync(category.Id);
            var categories = await categoryService.GetAllCategoriesAsync();

            string duplicate;
            try
            {
                await categoryService.AddCategoryAsync(new Category { Name = name });
                duplicate = "FAILED (لم يُرفض)";
            }
            catch (InvalidOperationException)
            {
                duplicate = "OK (تم الرفض)";
            }

            MessageBox.Show(
                $"عدد الفئات: {categories.Count}\n" +
                $"منتجات الفئة الجديدة: {products.Count}  (المتوقع 1)\n" +
                $"رفض الفئة المكررة: {duplicate}",
                "Smoke Test");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Smoke Test FAILED");
        }
    }

    private void SwitchLanguage_Click(object sender, RoutedEventArgs e)
    {
        LanguageManager.ToggleLanguage();

        var oldWindow = System.Windows.Application.Current.MainWindow;
        var newWindow = new MainWindow();

        System.Windows.Application.Current.MainWindow = newWindow;
        newWindow.Show();
        oldWindow?.Close();
    }
}