namespace GoogleDriveClone.Shared.Utils;

/// <summary>
/// ������ ��� ������ � �������
/// </summary>
public static class FileUtils
{
    /// <summary>
    /// ������� ����� ����� � ������� ��� ������� ������
    /// </summary>
    /// <param name="bytes">����� � ������</param>
    /// <returns>³������������� ����� (���������: "1.5 ��")</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "�", "��", "��", "��", "��" };
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