using System.ComponentModel.DataAnnotations;

namespace GoogleDriveClone.SharedModels.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Email � ����'�������")]
    [EmailAddress(ErrorMessage = "����������� ������ email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "������ � ����'�������")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "������ �� ������ �� 6 �� 100 �������")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "ϳ����������� ������ � ����'�������")]
    [Compare("Password", ErrorMessage = "����� �� ���������")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "��'� ����������� � ����'�������")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "��'� ����������� �� ������ �� 3 �� 50 �������")]
    public string Username { get; set; } = string.Empty;
}