namespace GoogleDriveClone.Shared.Utils;

/// <summary>
/// Утиліти для роботи з файлами
/// </summary>
public static class FileUtils
{
    /// <summary>
    /// Форматує розмір файлу в зручний для читання формат
    /// </summary>
    /// <param name="bytes">Розмір в байтах</param>
    /// <returns>Відформатований розмір (наприклад: "1.5 МБ")</returns>
    public static string FormatFileSize(long bytes)
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