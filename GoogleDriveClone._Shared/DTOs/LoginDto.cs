using System.ComponentModel.DataAnnotations;

namespace GoogleDriveClone.SharedModels.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Некоректний формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль є обов'язковим")]
    public string Password { get; set; } = string.Empty;
}