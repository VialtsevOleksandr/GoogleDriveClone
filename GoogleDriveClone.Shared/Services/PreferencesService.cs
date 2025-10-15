using Blazored.LocalStorage;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Shared.Services;

public interface IPreferencesService
{
    Task<Result<UserPreferences>> GetPreferencesAsync();
    Task<Result> SavePreferencesAsync(UserPreferences preferences);
    Task<Result> ClearPreferencesAsync();
}

public class UserPreferences
{
    public string SelectedFilter { get; set; } = "all";
    public bool ShowMetadata { get; set; } = true;
    public bool SortAscending { get; set; } = false;
    public string ViewMode { get; set; } = "grid"; // grid, list
    public bool GamingEffectsEnabled { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class PreferencesService : IPreferencesService
{
    private readonly ILocalStorageService _localStorage;
    private const string PreferencesKey = "gamingDrivePreferences";

    public PreferencesService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<Result<UserPreferences>> GetPreferencesAsync()
    {
        try
        {
            var preferences = await _localStorage.GetItemAsync<UserPreferences>(PreferencesKey);
            return preferences ?? new UserPreferences();
        }
        catch (Exception)
        {
            // Повертаємо дефолтні налаштування якщо не вдалося завантажити
            return new UserPreferences();
        }
    }

    public async Task<Result> SavePreferencesAsync(UserPreferences preferences)
    {
        try
        {
            preferences.LastUpdated = DateTime.UtcNow;
            await _localStorage.SetItemAsync(PreferencesKey, preferences);
            return Result.Success();
        }
        catch (Exception)
        {
            // Ігноруємо помилку збереження - це не критично
            return Result.Success();
        }
    }

    public async Task<Result> ClearPreferencesAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync(PreferencesKey);
            return Result.Success();
        }
        catch (Exception)
        {
            // Ігноруємо помилку очищення - це не критично  
            return Result.Success();
        }
    }
}