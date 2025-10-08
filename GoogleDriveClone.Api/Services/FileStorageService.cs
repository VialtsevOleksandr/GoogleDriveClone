using GoogleDriveClone.Api.Interfaces;

namespace GoogleDriveClone.Api.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public FileStorageService(IConfiguration configuration)
    {
        _storagePath = configuration["FileStorageSettings:StoragePath"] ?? "Files";
        
        // Створюємо головну папку якщо не існує
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task SaveFileAsync(Stream fileStream, string storedFileName, string userId)
    {
        var userFolderPath = Path.Combine(_storagePath, userId);
        Directory.CreateDirectory(userFolderPath); // Не кидає помилку, якщо папка вже існує

        var filePath = Path.Combine(userFolderPath, storedFileName);

        using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOutput);
    }

    public Task<Stream?> GetFileAsync(string storedFileName, string userId)
    {
        var userFolderPath = Path.Combine(_storagePath, userId);
        var filePath = Path.Combine(userFolderPath, storedFileName);

        if (!File.Exists(filePath))
        {
            // Повертаємо завершений Task з результатом null
            return Task.FromResult<Stream?>(null);
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteFileAsync(string storedFileName, string userId)
    {
        var userFolderPath = Path.Combine(_storagePath, userId);
        var filePath = Path.Combine(userFolderPath, storedFileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public async Task DeleteFilesAsync(IEnumerable<string> storedFileNames, string userId)
    {
        var userFolder = Path.Combine(_storagePath, userId);
        
        foreach (var fileName in storedFileNames)
        {
            var filePath = Path.Combine(userFolder, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        
        // Видаляємо папку користувача, якщо вона стала порожньою
        if (Directory.Exists(userFolder) && !Directory.EnumerateFileSystemEntries(userFolder).Any())
        {
            Directory.Delete(userFolder);
        }
        
        await Task.CompletedTask;
    }
}