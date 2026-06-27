using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMoney.App.ViewModels;
using OpenMoney.Data;
using OpenMoney.Data.Repositories;
using System.Windows;

namespace OpenMoney.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, config);
        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")!;

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseMySql(connectionString, serverVersion),
            ServiceLifetime.Transient);

        services.AddTransient<AccountRepository>();
        services.AddTransient<InvestmentRepository>();
        services.AddTransient<TransactionRepository>();

        services.AddTransient<PortfolioViewModel>();
        services.AddTransient<AccountViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainViewModel>();
    }
}
