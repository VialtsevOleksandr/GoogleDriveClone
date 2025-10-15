using Microsoft.JSInterop;

namespace GoogleDriveClone.Shared.Services;

public interface IApiConfigService
{
    Task InitializeAsync();
    string GetApiBaseUrl();
}

public class ApiConfigService : IApiConfigService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private string _apiBaseUrl = "";

    public ApiConfigService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        // Get base URL from HttpClient (configured in MauiProgram.cs or Program.cs)
        var baseAddress = _httpClient.BaseAddress?.ToString() ?? "";
        
        // Remove trailing slash if present
        if (baseAddress.EndsWith("/"))
        {
            baseAddress = baseAddress.TrimEnd('/');
        }
        
        _apiBaseUrl = baseAddress;
        
        // Pass to JavaScript
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", 
                $"window.__GAMING_DRIVE_API_BASE__ = '{_apiBaseUrl}';");
        }
        catch
        {
            // Ignore errors during initialization
        }
    }

    public string GetApiBaseUrl()
    {
        return _apiBaseUrl;
    }
}
