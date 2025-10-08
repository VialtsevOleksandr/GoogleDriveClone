using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GoogleDriveClone.SharedModels.DTOs;

// DTO для відповіді з інформацією про один файл
public class FileResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
}

// Клас для результату завантаження файлу
public class FileDownloadResult
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
}

// DTO для завантаження. [FromForm] в контролері буде очікувати поле з іменем "file"
public record UploadFileDto(IFormFile File);

// DTO для отримання ід файлів для видалення
public class BatchDeleteRequest
{
    public List<string> FileIds { get; set; } = new List<string>();
}

// DTO для отримання змін до файлу
public class UpdateFileContentRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
}