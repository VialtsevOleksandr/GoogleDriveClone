using GoogleDriveClone.Api.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace GoogleDriveClone.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FilesController : ApiControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// Завантаження файлу
    /// </summary>
    /// <param name="uploadDto">Файл для завантаження</param>
    /// <returns>Інформація про завантажений файл</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileDto uploadDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.UploadFileAsync(uploadDto.File, userId);
        return HandleResult(result, "Файл успішно завантажено");
    }

    /// <summary>
    /// Отримання списку файлів користувача
    /// </summary>
    /// <returns>Список файлів користувача</returns>
    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.GetUserFilesAsync(userId);
        return HandleResult(result, "Список файлів отримано успішно");
    }

    /// <summary>
    /// Отримання інформації про конкретний файл
    /// </summary>
    /// <param name="id">ID файлу</param>
    /// <returns>Інформація про файл</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.GetFileByIdAsync(id, userId);
        return HandleResult(result, "Інформація про файл отримана успішно");
    }

    /// <summary>
    /// Завантаження файлу на локальний комп'ютер
    /// </summary>
    /// <param name="id">ID файлу</param>
    /// <returns>Файл для завантаження</returns>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.DownloadFileAsync(id, userId);

        // Обробляємо помилку окремо, бо успішний результат - це файл, а не JSON
        if (!result.IsSuccess)
        {
            return HandleResult(result, string.Empty); // Використовуємо наш базовий обробник помилок
        }

        var downloadResult = result.Value!;
        // Повертаємо файл. ASP.NET Core сам подбає про правильні заголовки.
        return File(downloadResult.Content, downloadResult.ContentType, downloadResult.OriginalName);
    }

    /// <summary>
    /// Видалення файлу
    /// </summary>
    /// <param name="id">ID файлу</param>
    /// <returns>Підтвердження видалення</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.DeleteFileAsync(id, userId);
        return HandleResult(result, "Файл успішно видалено");
    }

    /// <summary>
    /// Пакетне видалення файлів
    /// </summary>
    /// <param name="request">Список ID файлів для видалення</param>
    /// <returns>Підтвердження видалення</returns>
    [HttpPost("delete-batch")]
    public async Task<IActionResult> DeleteFiles([FromBody] BatchDeleteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.DeleteFilesAsync(request.FileIds, userId);
        return HandleResult(result, $"Файли успішно видалено ({request.FileIds.Count} шт.)");
    }

    /// <summary>
    /// Оновлення вмісту файлу
    /// </summary>
    /// <param name="id">ID файлу</param>
    /// <param name="request">Новий вміст файлу</param>
    /// <returns>Оновлена інформація про файл</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFileContent(string id, [FromBody] UpdateFileContentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        // Конвертуємо string у MemoryStream
        var contentBytes = Encoding.UTF8.GetBytes(request.Content);

        // Розраховуємо SHA-256 хеш в контролері
        string newFileHash;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(contentBytes);
            newFileHash = Convert.ToHexString(hashBytes).ToLower();
        }

        using var contentStream = new MemoryStream(contentBytes);

        // Передаємо в сервіс і потік, і вже готовий хеш
        var result = await _fileService.UpdateFileContentAsync(id, userId, contentStream, newFileHash);

        return HandleResult(result, "Вміст файлу успішно оновлено");
    }

    /// <summary>
    /// Отримання статистики користувача
    /// </summary>
    /// <returns>Статистика користувача</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetUserStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _fileService.GetUserStatsAsync(userId);
        return HandleResult(result, "Статистика користувача отримана успішно");
    }
}