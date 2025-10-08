using GoogleDriveClone.Shared.Auth;
using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleDriveClone.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        // Authentication services - правильна реєстрація
        services.AddScoped<CustomAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider => 
            provider.GetRequiredService<CustomAuthenticationStateProvider>());
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // File services
        services.AddScoped<IFileManagerService, FileManagerService>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IFileDownloadService, FileDownloadService>();
        
        // User services
        services.AddScoped<IUserStatsService, UserStatsService>();

        // Utility services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPreferencesService, PreferencesService>();

        return services;
    }
}