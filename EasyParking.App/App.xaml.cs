using System.IO;
using System.Windows;
using EasyParking.App.Services;
using EasyParking.App.ViewModels;
using EasyParking.Data;
using EasyParking.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyParking.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dataDir = Path.Combine(AppContext.BaseDirectory);
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "easyparking.db");

        var services = new ServiceCollection();

        services.AddDbContext<EasyParkingDbContext>(
            opt => opt.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Transient);

        services.AddSingleton<ITarifRechner, TarifRechner>();
        services.AddSingleton<IParkplatzZuteiler, ParkplatzZuteiler>();
        services.AddSingleton<IMietPruefer, MietPruefer>();
        services.AddSingleton<TarifValidator>();
        services.AddSingleton<ParkhausValidator>();
        services.AddSingleton<IZahlungsdienst, DummyZahlungsdienst>();
        services.AddSingleton<IBuchhaltungsService, DummyBuchhaltungsService>();

        services.AddTransient<ParkhausService>();
        services.AddTransient<StatistikService>();
        services.AddTransient<TarifVerwaltungService>();
        services.AddTransient<FeiertagService>();
        services.AddTransient<MainViewModel>();

        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EasyParkingDbContext>();
            Seeder.EnsureSeeded(db);
        }

        var main = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        main.Show();
    }
}
