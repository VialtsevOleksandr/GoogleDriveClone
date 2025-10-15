using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.SharedModels.Results;
using Microsoft.JSInterop;
using System.Text.Json;

namespace GoogleDriveClone.Shared.Services;

public class SyncService : ISyncService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IFileManagerService _fileManagerService;
    private readonly INotificationService _notificationService;

    public SyncService(
        IJSRuntime jsRuntime, 
        IFileManagerService fileManagerService,
        INotificationService notificationService)
    {
        _jsRuntime = jsRuntime;
        _fileManagerService = fileManagerService;
        _notificationService = notificationService;
    }

    public async Task<Result<bool>> IsFolderSyncSupportedAsync()
    {
        try
        {
            var isSupported = await _jsRuntime.InvokeAsync<bool>("isFolderSyncSupported");
            return isSupported;
        }
        catch (Exception ex)
        {
            return DomainErrors.Sync.CheckSupportFailed;
        }
    }

    public async Task<Result<FolderSyncResult>> SynchronizeFolderAsync()
    {
        try {
            // 1. Перевіряємо підтримку
            var supportResult = await IsFolderSyncSupportedAsync();
            if (!supportResult.IsSuccess || !supportResult.Value)
            {
                return DomainErrors.Sync.NotSupported;
            }

            // 2. Показуємо діалог вибору папки
            JsonElement folderResult;
            try
            {
                folderResult = await _jsRuntime.InvokeAsync<JsonElement>("selectFolder");
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Помилка вибору папки: {ex.Message}");
                return DomainErrors.Sync.FolderSelectionFailed;
            }
            
            if (!folderResult.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
            {
                var error = folderResult.TryGetProperty("error", out var errorProp) 
                    ? errorProp.GetString() ?? "Невідома помилка"
                    : "Вибір папки скасовано";
                
                if (error.Contains("скасован") || error.Contains("cancel") || error.Contains("abort"))
                {
                    return DomainErrors.Sync.FolderSelectionCancelled;
                }
                    
                return DomainErrors.Sync.FolderSelectionFailed;
            }

            var folderName = folderResult.TryGetProperty("name", out var nameProp) 
                ? nameProp.GetString() ?? "Невідома папка"
                : "Невідома папка";

            // 3. Читаємо файли з папки (БЕЗ нотіфікацій)
            JsonElement readResult;
            try
            {
                readResult = await _jsRuntime.InvokeAsync<JsonElement>("readFolderFiles", "use-stored-handle", null);
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Помилка читання файлів: {ex.Message}");
                return DomainErrors.Sync.ReadFolderFailed;
            }

            if (!readResult.TryGetProperty("success", out var readSuccessProp) || !readSuccessProp.GetBoolean())
            {
                var errorMessage = "Невідома помилка при читанні папки";
                if (readResult.TryGetProperty("error", out var readErrorProp))
                {
                    errorMessage = readErrorProp.GetString() ?? errorMessage;
                }
                
                await _notificationService.ShowErrorAsync($"Помилка читання файлів: {errorMessage}");
                return DomainErrors.Sync.ReadFolderFailed;
            }

            var localFiles = ParseLocalFiles(readResult);
            var totalFiles = readResult.TryGetProperty("totalFiles", out var totalProp) ? totalProp.GetInt32() : 0;
            var validFiles = localFiles.Count;

            if (localFiles.Count == 0)
            {
                return new FolderSyncResult
                {
                    TotalFiles = 0,
                    ErrorMessage = DomainErrors.Sync.NoSupportedFiles.Message
                };
            }

            // 4. Отримуємо список файлів з сервера
            var serverFilesResult = await _fileManagerService.GetFilesAsync();
            if (!serverFilesResult.IsSuccess)
            {
                await _notificationService.ShowErrorAsync($"Не вдалося отримати файли з сервера: {serverFilesResult.Error?.Message}");
                return DomainErrors.Sync.ServerFilesFailed;
            }

            var serverFiles = serverFilesResult.Value ?? new List<GoogleDriveClone.SharedModels.DTOs.FileResponseDto>();

            // 5. Порівнюємо файли та визначаємо дії
            JsonElement compareResult;
            try
            {
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = false, 
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
                };
                
                compareResult = await _jsRuntime.InvokeAsync<JsonElement>("compareFolderFiles", 
                    JsonSerializer.Serialize(localFiles, jsonOptions), 
                    JsonSerializer.Serialize(serverFiles, jsonOptions));
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Помилка порівняння файлів: {ex.Message}");
                return DomainErrors.Sync.CompareFailed;
            }

            if (!compareResult.TryGetProperty("success", out var compareSuccessProp) || !compareSuccessProp.GetBoolean())
            {
                var compareError = compareResult.TryGetProperty("error", out var compareErrorProp)
                    ? compareErrorProp.GetString() ?? "Невідома помилка"
                    : "Помилка порівняння файлів";
                
                await _notificationService.ShowErrorAsync($"Помилка порівняння: {compareError}");
                return DomainErrors.Sync.CompareFailed;
            }

            var syncActions = ParseSyncActions(compareResult);
            var summary = ParseSyncSummary(compareResult);

            // 6. Показуємо план дій користувачу
            if (syncActions.Count == 0)
            {
                await _notificationService.ShowSuccessAsync("Всі файли вже синхронізовані!");
                
                return new FolderSyncResult
                {
                    TotalFiles = summary.TotalLocalFiles,
                    UnchangedFiles = summary.UnchangedFiles,
                    SuccessCount = 0,
                    ErrorCount = 0
                };
            }

            // 7. Підтвердження від користувача (використовуємо КАСТОМНИЙ діалог)
            var confirmData = new
            {
                NewFiles = summary.NewFiles,
                ReplacedFiles = summary.ReplacedFiles,
                UnchangedFiles = summary.UnchangedFiles
            };

            bool confirmed;
            try
            {
                confirmed = await _jsRuntime.InvokeAsync<bool>("showSyncConfirmDialog", JsonSerializer.Serialize(confirmData));
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Помилка підтвердження: {ex.Message}");
                return DomainErrors.Sync.UserCancelled;
            }
            
            if (!confirmed)
            {
                return new FolderSyncResult
                {
                    TotalFiles = summary.TotalLocalFiles,
                    ErrorMessage = DomainErrors.Sync.UserCancelled.Message
                };
            }

            // 8. Виконуємо синхронізацію (БЕЗ нотіфікацій прогресу)
            JsonElement syncResult;
            try
            {
                var syncJsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = false, 
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                syncResult = await _jsRuntime.InvokeAsync<JsonElement>("performFolderSync", 
                    JsonSerializer.Serialize(syncActions, syncJsonOptions),
                    null); // Не передаємо progressCallback
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Помилка виконання синхронізації: {ex.Message}");
                return DomainErrors.Sync.ExecutionFailed;
            }

            if (!syncResult.TryGetProperty("success", out var syncSuccessProp) || !syncSuccessProp.GetBoolean())
            {
                var syncError = syncResult.TryGetProperty("error", out var syncErrorProp)
                    ? syncErrorProp.GetString() ?? "Невідома помилка"
                    : "Помилка виконання синхронізації";
                
                await _notificationService.ShowErrorAsync($"Помилка синхронізації: {syncError}");
                return DomainErrors.Sync.ExecutionFailed;
            }

            // 9. Обробляємо результати
            var results = ParseSyncResults(syncResult);
            var syncSummaryResult = ParseExecutionSummary(syncResult);

            var finalResult = new FolderSyncResult
            {
                TotalFiles = summary.TotalLocalFiles,
                NewFiles = summary.NewFiles,
                ReplacedFiles = summary.ReplacedFiles,
                UnchangedFiles = summary.UnchangedFiles,
                SuccessCount = syncSummaryResult.Success,
                ErrorCount = syncSummaryResult.Errors,
                Results = results
            };

            // 10. Показуємо ТІЛЬКИ фінальний результат
            if (syncSummaryResult.Errors == 0)
            {
                await _notificationService.ShowSuccessAsync(
                    $"Синхронізація завершена! Додано: {summary.NewFiles}, Оновлено: {summary.ReplacedFiles}");
            }
            else if (syncSummaryResult.Success > 0)
            {
                await _notificationService.ShowWarningAsync(
                    $"Синхронізація завершена з помилками. Успішно: {syncSummaryResult.Success}, Помилок: {syncSummaryResult.Errors}");
            }
            else
            {
                await _notificationService.ShowErrorAsync(
                    $"Синхронізація не вдалася. Всі файли з помилками.");
            }

            // Очищуємо збережені дані синхронізації
            try
            {
                await _jsRuntime.InvokeVoidAsync("clearSyncData");
            }
            catch (Exception cleanupEx)
            {
                Console.WriteLine($"Warning: Failed to cleanup sync data: {cleanupEx.Message}");
            }

            return finalResult;
        }
        catch (Exception ex)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("clearSyncData");
            }
            catch
            {
                // Ignore
            }

            await _notificationService.ShowErrorAsync($"Критична помилка синхронізації: {ex.Message}");
            return DomainErrors.Sync.CriticalError;
        }
    }

    private List<LocalFileInfo> ParseLocalFiles(JsonElement readResult)
    {
        var files = new List<LocalFileInfo>();
        
        if (readResult.TryGetProperty("files", out var filesArray))
        {
            foreach (var fileElement in filesArray.EnumerateArray())
            {
                var file = new LocalFileInfo
                {
                    Name = fileElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "",
                    Size = fileElement.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0,
                    Hash = fileElement.TryGetProperty("hash", out var hashProp) ? hashProp.GetString() ?? "" : ""
                };
                
                if (!string.IsNullOrEmpty(file.Name) && !string.IsNullOrEmpty(file.Hash))
                {
                    files.Add(file);
                }
            }
        }
        
        return files;
    }

    private List<SyncAction> ParseSyncActions(JsonElement compareResult)
    {
        var actions = new List<SyncAction>();
        
        if (compareResult.TryGetProperty("actions", out var actionsArray))
        {
            foreach (var actionElement in actionsArray.EnumerateArray())
            {
                var action = new SyncAction
                {
                    Type = actionElement.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "" : "",
                    FileName = actionElement.TryGetProperty("fileName", out var nameProp) ? nameProp.GetString() ?? "" : "",
                    Reason = actionElement.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() ?? "" : ""
                };

                if (actionElement.TryGetProperty("localFile", out var localFileProp))
                {
                    action.LocalFile = new LocalFileInfo
                    {
                        Name = localFileProp.TryGetProperty("name", out var localNameProp) ? localNameProp.GetString() ?? "" : "",
                        Size = localFileProp.TryGetProperty("size", out var localSizeProp) ? localSizeProp.GetInt64() : 0,
                        Hash = localFileProp.TryGetProperty("hash", out var localHashProp) ? localHashProp.GetString() ?? "" : ""
                    };
                }

                if (actionElement.TryGetProperty("serverFile", out var serverFileProp))
                {
                    // Шукаємо ID в різних форматах (camelCase та PascalCase)
                    action.ServerFileId = serverFileProp.TryGetProperty("id", out var idProp) 
                        ? idProp.GetString() 
                        : serverFileProp.TryGetProperty("Id", out var idPropAlt) 
                            ? idPropAlt.GetString() 
                            : null;
                }
                
                // Також перевіряємо чи є serverFileId як окреме поле
                if (string.IsNullOrEmpty(action.ServerFileId) && actionElement.TryGetProperty("serverFileId", out var serverFileIdProp))
                {
                    action.ServerFileId = serverFileIdProp.GetString();
                }
                
                actions.Add(action);
            }
        }
        
        return actions;
    }

    private SyncSummary ParseSyncSummary(JsonElement compareResult)
    {
        var summary = new SyncSummary();
        
        if (compareResult.TryGetProperty("summary", out var summaryElement))
        {
            summary.TotalLocalFiles = summaryElement.TryGetProperty("totalLocalFiles", out var totalProp) ? totalProp.GetInt32() : 0;
            summary.NewFiles = summaryElement.TryGetProperty("newFiles", out var newProp) ? newProp.GetInt32() : 0;
            summary.ReplacedFiles = summaryElement.TryGetProperty("replacedFiles", out var replacedProp) ? replacedProp.GetInt32() : 0;
            summary.UnchangedFiles = summaryElement.TryGetProperty("unchangedFiles", out var unchangedProp) ? unchangedProp.GetInt32() : 0;
        }
        
        return summary;
    }

    private List<SyncResultItem> ParseSyncResults(JsonElement syncResult)
    {
        var results = new List<SyncResultItem>();
        
        if (syncResult.TryGetProperty("results", out var resultsArray))
        {
            foreach (var resultElement in resultsArray.EnumerateArray())
            {
                var result = new SyncResultItem
                {
                    FileName = resultElement.TryGetProperty("fileName", out var nameProp) ? nameProp.GetString() ?? "" : "",
                    Action = resultElement.TryGetProperty("action", out var actionProp) ? actionProp.GetString() ?? "" : "",
                    Success = resultElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean(),
                    Error = resultElement.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : null
                };
                
                results.Add(result);
            }
        }
        
        return results;
    }

    private (int Success, int Errors) ParseExecutionSummary(JsonElement syncResult)
    {
        if (syncResult.TryGetProperty("summary", out var summaryElement))
        {
            var success = summaryElement.TryGetProperty("success", out var successProp) ? successProp.GetInt32() : 0;
            var errors = summaryElement.TryGetProperty("errors", out var errorsProp) ? errorsProp.GetInt32() : 0;
            return (success, errors);
        }
        
        return (0, 0);
    }

    // Helper class for progress callbacks
    public class ProgressCallback
    {
        private readonly INotificationService _notificationService;

        public ProgressCallback(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [JSInvokable]
        public async Task UpdateProgress(JsonElement progress)
        {
            try
            {
                if (progress.TryGetProperty("message", out var messageProp))
                {
                    var message = messageProp.GetString();
                    if (!string.IsNullOrEmpty(message))
                    {
                        await _notificationService.ShowInfoAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Логуємо помилку але не кидаємо виключення
                Console.WriteLine($"Error in progress callback: {ex.Message}");
            }
        }
    }
}