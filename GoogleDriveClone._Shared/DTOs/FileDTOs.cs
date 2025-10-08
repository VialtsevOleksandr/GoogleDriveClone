using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GoogleDriveClone.SharedModels.DTOs;

// DTO ��� ������ � ����������� ��� ���� ����
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

// ���� ��� ���������� ������������ �����
public class FileDownloadResult
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
}

// DTO ��� ������������. [FromForm] � ��������� ���� ��������� ���� � ������ "file"
public record UploadFileDto(IFormFile File);

// DTO ��� ��������� �� ����� ��� ���������
public class BatchDeleteRequest
{
    public List<string> FileIds { get; set; } = new List<string>();
}

// DTO ��� ��������� ��� �� �����
public class UpdateFileContentRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
}