using GoogleDriveClone.Api.Data;
using GoogleDriveClone.Api.Entities;
using GoogleDriveClone.Api.Interfaces;
using GoogleDriveClone.SharedModels.Results;
using Microsoft.EntityFrameworkCore;

namespace GoogleDriveClone.Api.Repositories;

public class FileRepository : IFileRepository
{
    private readonly AppDbContext _context;

    public FileRepository(AppDbContext context)
    {
        _context = context;
    }

    public Result Create(FileMetadata file)
    {
        _context.Files.Add(file);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<FileMetadata>>> GetByUserIdAsync(string userId)
    {
        var files = await _context.Files
            .Include(f => f.Owner)
            .Where(f => f.OwnerId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return files;
    }

    public async Task<Result<FileMetadata>> GetByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return DomainErrors.File.InvalidId;
        }

        var file = await _context.Files
            .Include(f => f.Owner)
            .FirstOrDefaultAsync(f => f.Id == guid);

        if (file == null)
        {
            return DomainErrors.File.NotFound;
        }

        return file;
    }

    public async Task<Result<FileMetadata>> GetOwnedFileByIdAsync(string id, string userId)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return DomainErrors.File.InvalidId;
        }

        var file = await _context.Files
            .Include(f => f.Owner)
            .FirstOrDefaultAsync(f => f.Id == guid && f.OwnerId == userId);

        if (file == null)
        {
            return DomainErrors.File.NotFound;
        }

        return file;
    }

    public Result Delete(FileMetadata file)
    {
        _context.Files.Remove(file);
        return Result.Success();
    }

    public async Task<Result> DeleteFilesAsync(IEnumerable<FileMetadata> files)
    {
        _context.Files.RemoveRange(files);
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public Result Update(FileMetadata file)
    {
        _context.Files.Update(file);
        return Result.Success();
    }
}