using Microsoft.Extensions.Logging;
using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.Shared.Auth;
using GoogleDriveClone.Shared.Extensions;
using GoogleDriveClone.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

namespace GoogleDriveClone;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Базова адреса API (можна винести в конфігурацію пізніше)
        var apiBaseAddress = "https://localhost:7166/";

        // Configure HttpClient for API services
        builder.Services.AddHttpClient<IAuthApiService, AuthApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
        });

        // Configure default HttpClient for other services
        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
        });

        builder.Services.AddScoped<HttpClient>(provider =>
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

        // Add LocalStorage for MAUI Blazor
        builder.Services.AddBlazoredLocalStorage();

        // Add Authorization 
        builder.Services.AddAuthorizationCore();

        // Add all shared services (включає authentication)
        builder.Services.AddSharedServices();

        // Add MAUI-specific services
        builder.Services.AddSingleton<MauiMenuService>();

        return builder.Build();
    }
}
