namespace GoogleDriveClone.SharedModels.DTOs;

// DTO ��� ���������� �����������
public class UserStatsDto
{
    // ʳ������ ����� �����������
    public int TotalFiles { get; set; }
    
    // ��������� ����� ����� � ������
    public long TotalSizeBytes { get; set; }
    
    // ������������ ����� ��� �����������
    public string FormattedSize => FormatFileSize(TotalSizeBytes);
    
    // ������������ ������ �����
    private static string FormatFileSize(long bytes)
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