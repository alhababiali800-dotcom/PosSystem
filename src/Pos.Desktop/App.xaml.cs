using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Interfaces;
using Pos.Application.UseCases;
using Pos.Desktop.Localization;
using Pos.Infrastructure.Data;
using Pos.Desktop.Navigation; // تمت الإضافة
using Pos.Desktop.ViewModels; // تمت الإضافة

namespace Pos.Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        LanguageManager.SetLanguage(LanguageManager.Arabic);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<PosDbContext>().Database.Migrate();
        }

        base.OnStartup(e);   // opens nothing now that StartupUri is gone

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSystem");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "pos_database.db");

        // Infrastructure
        services.AddDbContext<PosDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PosDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Application
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuthService, AuthService>();

        // Windows
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();

        // Navigation + ViewModels
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<ProductsViewModel>();
    }
}