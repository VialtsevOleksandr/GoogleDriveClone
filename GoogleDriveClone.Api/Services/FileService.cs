using GoogleDriveClone.Api.Data;
using GoogleDriveClone.Api.Entities;
using GoogleDriveClone.Api.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace GoogleDriveClone.Api.Services;

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public FileService(
        IFileRepository fileRepository,
        IFileStorageService fileStorageService,
        UserManager<User> userManager,
        IConfiguration configuration,
        AppDbContext context)
    {
        _fileRepository = fileRepository;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
    }

    public async Task<Result<FileResponseDto>> UploadFileAsync(IFormFile file, string userId)
    {
        // 1. �������� �����
        if (file == null || file.Length == 0)
        {
            return new Error("File.EmptyFile", "���� ������� ��� �� ������ �����.", ErrorType.Validation);
        }

        // �������� ������ �����
        var maxFileSizeMB = _configuration.GetValue<int>("FileStorageSettings:MaxFileSizeMB", 50);
        if (file.Length > maxFileSizeMB * 1024 * 1024)
        {
            return DomainErrors.File.InvalidFileSize;
        }

        // �������� ���� �����
        var allowedTypes = _configuration.GetSection("FileStorageSettings:AllowedFileTypes").Get<string[]>();
        if (allowedTypes != null && allowedTypes.Length > 0)
        {
            var fileExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            if (!allowedTypes.Any(t => t.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            {
                return DomainErrors.File.InvalidFileType;
            }
        }

        // �������� �����������
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return DomainErrors.User.NotFound;
        }

        // ������� ���� � MemoryStream ��� ��������� ���������� �������
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        // ����������� SHA-256 ���
        string fileHash;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(memoryStream);
            fileHash = Convert.ToHexString(hashBytes).ToLower();
        }

        // ��������� ������� ������ �� ������� ��� ����������
        memoryStream.Position = 0;

        // 2. ��������� ������� FileMetadata
        var createdAt = DateTime.UtcNow;
        var fileMetadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            OriginalName = file.FileName,
            Size = file.Length,
            ContentType = file.ContentType ?? "application/octet-stream",
            CreatedAt = createdAt,
            ModifiedAt = createdAt, // ��� ������� ����������� ���� ���������
            FileHash = fileHash,
            OwnerId = userId,
            Owner = user
        };

        // 3. ������� ��'� ��� ��������� �� ����� �� ����� ������������� ID
        var storedFileName = $"{fileMetadata.Id}{Path.GetExtension(file.FileName)}";

        // --- ���� ��� �������� (Unit of Work) ---
        try
        {
            // 4. �������� ���� �� ����
            await _fileStorageService.SaveFileAsync(memoryStream, storedFileName, userId);

            // 5. ������ ����� �� ���������� � �� (���������)
            var createResult = _fileRepository.Create(fileMetadata);
            if (!createResult.IsSuccess)
            {
                // ��������� ��� ���������� ����
                await _fileStorageService.DeleteFileAsync(storedFileName, userId);
                return createResult.Error!;
            }
            
            // 6. �������� ���� � ��. ���� ��� ���� �������, �� ��������� � catch
            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            // ���� ���������� � �� �� �������, ��������� ��� ���������� ����
            await _fileStorageService.DeleteFileAsync(storedFileName, userId);
            return DomainErrors.File.UploadFailed;
        }

        // 7. ��������� ������� ���������
        return new FileResponseDto
        {
            Id = fileMetadata.Id.ToString(),
            OriginalName = fileMetadata.OriginalName,
            Size = fileMetadata.Size,
            ContentType = fileMetadata.ContentType,
            CreatedAt = fileMetadata.CreatedAt,
            ModifiedAt = fileMetadata.ModifiedAt,
            OwnerUsername = user.UserName!,
            FileHash = fileMetadata.FileHash
        };
    }

    public async Task<Result<IEnumerable<FileResponseDto>>> GetUserFilesAsync(string userId)
    {
        var filesResult = await _fileRepository.GetByUserIdAsync(userId);
        if (!filesResult.IsSuccess)
        {
            return filesResult.Error!;
        }

        var fileResponseDtos = filesResult.Value!.Select(f => new FileResponseDto
        {
            Id = f.Id.ToString(),
            OriginalName = f.OriginalName,
            Size = f.Size,
            ContentType = f.ContentType,
            CreatedAt = f.CreatedAt,
            ModifiedAt = f.ModifiedAt,
            OwnerUsername = f.Owner?.UserName ?? "�������",
            FileHash = f.FileHash ?? string.Empty
        }).ToList();

        return fileResponseDtos;
    }

    public async Task<Result<FileResponseDto>> GetFileByIdAsync(string fileId, string userId)
    {
        var fileResult = await _fileRepository.GetOwnedFileByIdAsync(fileId, userId);
        if (!fileResult.IsSuccess)
        {
            return fileResult.Error!;
        }

        var file = fileResult.Value!;
        return new FileResponseDto
        {
            Id = file.Id.ToString(),
            OriginalName = file.OriginalName,
            Size = file.Size,
            ContentType = file.ContentType,
            CreatedAt = file.CreatedAt,
            ModifiedAt = file.ModifiedAt,
            OwnerUsername = file.Owner?.UserName ?? "�������",
            FileHash = file.FileHash ?? string.Empty
        };
    }

    public async Task<Result> DeleteFileAsync(string fileId, string userId)
    {
        var fileResult = await _fileRepository.GetOwnedFileByIdAsync(fileId, userId);
        if (!fileResult.IsSuccess)
        {
            return fileResult.Error!;
        }

        var file = fileResult.Value!;

        // ������� ��'� ����� �� �����
        var storedFileName = $"{file.Id}{Path.GetExtension(file.OriginalName)}";

        // ��������� ���� � ����� �����������
        await _fileStorageService.DeleteFileAsync(storedFileName, userId);
        
        // ������ ��������� � ��������� (���������)
        var deleteResult = _fileRepository.Delete(file);
        if (!deleteResult.IsSuccess)
        {
            return deleteResult.Error!;
        }

        // �������� �� ���� �� ����� ����������
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<FileDownloadResult>> DownloadFileAsync(string fileId, string userId)
    {
        var fileResult = await _fileRepository.GetOwnedFileByIdAsync(fileId, userId);
        if (!fileResult.IsSuccess)
        {
            return fileResult.Error!;
        }

        var file = fileResult.Value!;

        // ������� ��'� ����� �� �����
        var storedFileName = $"{file.Id}{Path.GetExtension(file.OriginalName)}";

        // �������� ���� � ����� - ����� Stream
        var stream = await _fileStorageService.GetFileAsync(storedFileName, userId);
        if (stream == null)
        {
            return DomainErrors.File.NotFound;
        }

        return new FileDownloadResult
        {
            Content = stream,
            ContentType = file.ContentType,
            OriginalName = file.OriginalName
        };
    }

    // ���������� ����������� - ����� ������� ����� � �����
    public async Task<Result<UserStatsDto>> GetUserStatsAsync(string userId)
    {
        var filesResult = await _fileRepository.GetByUserIdAsync(userId);
        if (!filesResult.IsSuccess)
        {
            return filesResult.Error!;
        }

        var files = filesResult.Value!;
        var stats = new UserStatsDto
        {
            TotalFiles = files.Count(),
            TotalSizeBytes = files.Sum(f => f.Size)
        };

        return stats;
    }

    // ��� ������
    public async Task<Result> DeleteFilesAsync(IEnumerable<string> fileIds, string userId)
    {
        var filesList = new List<FileMetadata>();
        
        // �������� �� ����� �� ID �� ���������� ����� ��������
        foreach (var fileId in fileIds)
        {
            var fileResult = await _fileRepository.GetOwnedFileByIdAsync(fileId, userId);
            if (fileResult.IsSuccess)
            {
                filesList.Add(fileResult.Value!);
            }
        }

        if (!filesList.Any())
        {
            return DomainErrors.File.NotFound;
        }

        // ������� ������ ���� ����� ��� ��������� ���������
        var storedFileNames = filesList.Select(file => 
            $"{file.Id}{Path.GetExtension(file.OriginalName)}"
        ).ToList();

        // ��������� �� ������ ����� ����� ��������
        await _fileStorageService.DeleteFilesAsync(storedFileNames, userId);

        // ��������� ������ � ���� ����� ����� �������
        var deleteResult = await _fileRepository.DeleteFilesAsync(filesList);
        if (!deleteResult.IsSuccess)
        {
            return deleteResult.Error!;
        }

        return Result.Success();
    }

    public async Task<Result<FileResponseDto>> UpdateFileContentAsync(string fileId, string userId, Stream newContentStream, string newFileHash)
    {
        // �������� ������� �����
        var fileResult = await _fileRepository.GetOwnedFileByIdAsync(fileId, userId);
        if (!fileResult.IsSuccess)
        {
            return fileResult.Error!;
        }

        var file = fileResult.Value!;
        var storedFileName = $"{file.Id}{Path.GetExtension(file.OriginalName)}";

        // ������������ �������� ����
        newContentStream.Position = 0; // ������������ �� ���� �� �������
        await _fileStorageService.SaveFileAsync(newContentStream, storedFileName, userId);

        // ��������� ������� � ��������� �����
        file.ModifiedAt = DateTime.UtcNow;
        file.FileHash = newFileHash;
        file.Size = newContentStream.Length;

        // �������� ���� � ��
        var updateResult = _fileRepository.Update(file);
        if (!updateResult.IsSuccess)
        {
            return updateResult.Error!;
        }

        await _context.SaveChangesAsync();

        // ��������� ������� �������
        return new FileResponseDto
        {
            Id = file.Id.ToString(),
            OriginalName = file.OriginalName,
            Size = file.Size,
            ContentType = file.ContentType,
            CreatedAt = file.CreatedAt,
            ModifiedAt = file.ModifiedAt,
            OwnerUsername = file.Owner?.UserName ?? "�������",
            FileHash = file.FileHash ?? string.Empty
        };
    }
}