using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace GoogleDriveClone.Shared.Auth;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    // Головний метод, який Blazor викликає, щоб дізнатися, хто поточний користувач
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(savedToken))
        {
            // Якщо токена немає - користувач анонімний
            ClearHttpClientAuth();
            return CreateAnonymousState();
        }

        // Валідуємо токен перед використанням
        if (!IsValidJwtToken(savedToken))
        {
            // Якщо токен невалідний - очищуємо його та повертаємо анонімного користувача
            await _localStorage.RemoveItemAsync("authToken");
            ClearHttpClientAuth();
            return CreateAnonymousState();
        }

        // Встановлюємо заголовок авторизації для всіх наступних запитів HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);

        try
        {
            // Створюємо "посвідчення" користувача на основі даних з токена
            var claims = ParseClaimsFromJwt(savedToken);
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            return new AuthenticationState(claimsPrincipal);
        }
        catch (Exception)
        {
            // Якщо не вдалося розпарсити токен - очищуємо все
            await _localStorage.RemoveItemAsync("authToken");
            ClearHttpClientAuth();
            return CreateAnonymousState();
        }
    }

    // Цей метод ви будете викликати зі сторінки логіну після успішного входу
    public async Task MarkUserAsAuthenticated(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        await _localStorage.SetItemAsync("authToken", token);
        
        // Встановлюємо заголовок авторизації
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Отримуємо новий стан автентифікації
        var authState = GetAuthenticationStateAsync();
        
        // Повідомляємо Blazor, що стан автентифікації змінився
        NotifyAuthenticationStateChanged(authState);
    }

    // Цей метод ви будете викликати для виходу з системи
    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync("authToken");
        
        // Очищуємо HttpClient від авторизації
        ClearHttpClientAuth();
        
        var authState = Task.FromResult(CreateAnonymousState());
        NotifyAuthenticationStateChanged(authState);
    }

    // Додатковий метод для перевірки чи користувач автентифікований
    public async Task<bool> IsUserAuthenticatedAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }

    // Метод для отримання поточного токену
    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>("authToken");
    }

    // Приватні допоміжні методи
    private static AuthenticationState CreateAnonymousState()
    {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private void ClearHttpClientAuth()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private static bool IsValidJwtToken(string token)
    {
        try
        {
            // Базова перевірка структури JWT токену
            var parts = token.Split('.');
            return parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[1]);
        }
        catch
        {
            return false;
        }
    }

    // Допоміжний метод для розшифровки токена
    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];

        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));
            
            // Додаємо стандартні claims для кращої сумісності
            ExtractStandardClaims(keyValuePairs, claims);
        }
        
        return claims;
    }

    private static void ExtractStandardClaims(Dictionary<string, object> keyValuePairs, List<Claim> claims)
    {
        // Додаємо стандартні claims якщо їх немає
        if (keyValuePairs.ContainsKey("sub") && !claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, keyValuePairs["sub"].ToString()!));
        }
        
        if (keyValuePairs.ContainsKey("email") && !claims.Any(c => c.Type == ClaimTypes.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, keyValuePairs["email"].ToString()!));
        }
        
        if (keyValuePairs.ContainsKey("name") && !claims.Any(c => c.Type == ClaimTypes.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, keyValuePairs["name"].ToString()!));
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}