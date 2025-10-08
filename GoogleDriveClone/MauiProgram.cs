using Microsoft.Extensions.Logging;
using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.Shared.Auth;
using GoogleDriveClone.Shared.Extensions;
using GoogleDriveClone.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

namespace GoogleDriveClone
{
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

            // Configure HttpClient for API
            builder.Services.AddHttpClient<IAuthApiService, AuthApiService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7166/");
            });

            // Configure default HttpClient for other services
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7166/");
            });

            builder.Services.AddScoped<HttpClient>(provider =>
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

            // Add LocalStorage
            builder.Services.AddBlazoredLocalStorage();

            // Add Authentication
            builder.Services.AddScoped<CustomAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
                provider.GetRequiredService<CustomAuthenticationStateProvider>());
            builder.Services.AddAuthorizationCore();

            // Add Gaming Drive services
            builder.Services.AddSharedServices();

            return builder.Build();
        }
    }
}
