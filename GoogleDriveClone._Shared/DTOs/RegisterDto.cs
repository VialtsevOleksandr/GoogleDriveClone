using System.ComponentModel.DataAnnotations;

namespace GoogleDriveClone.SharedModels.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Некоректний формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль є обов'язковим")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити від 6 до 100 символів")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердження пароля є обов'язковим")]
    [Compare("Password", ErrorMessage = "Паролі не збігаються")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ім'я користувача є обов'язковим")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Ім'я користувача має містити від 3 до 50 символів")]
    public string Username { get; set; } = string.Empty;
}