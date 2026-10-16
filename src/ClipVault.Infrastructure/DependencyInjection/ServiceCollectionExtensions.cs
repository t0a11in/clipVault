using ClipVault.Core.Interfaces;
using ClipVault.Infrastructure.Data;
using ClipVault.Infrastructure.Repositories;
using ClipVault.Infrastructure.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClipVault.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClipVaultInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        services.AddDbContextFactory<ClipVaultDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<IClipRepository, ClipRepository>();
        services.AddSingleton<IClipboardMonitor, ClipboardMonitor>();
        services.AddSingleton<IHotkeyService, HotkeyService>();

        return services;
    }
}
