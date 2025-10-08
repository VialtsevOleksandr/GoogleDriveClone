namespace GoogleDriveClone.SharedModels.DTOs;

// DTO для статистики користувача
public class UserStatsDto
{
    // Кількість файлів користувача
    public int TotalFiles { get; set; }
    
    // Загальний розмір файлів в байтах
    public long TotalSizeBytes { get; set; }
    
    // Форматований розмір для відображення
    public string FormattedSize => FormatFileSize(TotalSizeBytes);
    
    // Форматування розміру файлу
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}