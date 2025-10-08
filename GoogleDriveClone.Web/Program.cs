using GoogleDriveClone.Web.Components;
using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.Shared.Auth;
using GoogleDriveClone.Shared.Extensions;
using GoogleDriveClone.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

namespace GoogleDriveClone.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Razor Components
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Отримуємо базову адресу API з конфігурації
        var apiBaseAddress = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7166/";

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

        // Add LocalStorage for Blazor Server
        builder.Services.AddBlazoredLocalStorage();

        // Add Authorization 
        builder.Services.AddAuthorizationCore();

        // Add all shared services (включає authentication)
        builder.Services.AddSharedServices();

        var app = builder.Build();

        // Configure pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        // Map components with Shared assembly
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(GoogleDriveClone.Shared._Imports).Assembly);

        app.Run();
    }
}