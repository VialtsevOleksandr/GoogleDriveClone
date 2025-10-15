using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Shared.Interfaces;

public interface ISyncService
{
    Task<Result<bool>> IsFolderSyncSupportedAsync();
    Task<Result<FolderSyncResult>> SynchronizeFolderAsync();
}

public class FolderSyncResult
{
    public int TotalFiles { get; set; }
    public int NewFiles { get; set; }
    public int ReplacedFiles { get; set; }
    public int UnchangedFiles { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<SyncResultItem> Results { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class SyncResultItem
{
    public string FileName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class LocalFileInfo
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
}

public class SyncAction
{
    public string Type { get; set; } = string.Empty; // "upload" or "replace"
    public string FileName { get; set; } = string.Empty;
    public LocalFileInfo? LocalFile { get; set; }
    public string? ServerFileId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class SyncSummary
{
    public int TotalLocalFiles { get; set; }
    public int NewFiles { get; set; }
    public int ReplacedFiles { get; set; }
    public int UnchangedFiles { get; set; }
}