using System.ComponentModel.DataAnnotations;

namespace GoogleDriveClone.SharedModels.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email � ����'�������")]
    [EmailAddress(ErrorMessage = "����������� ������ email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "������ � ����'�������")]
    public string Password { get; set; } = string.Empty;
}