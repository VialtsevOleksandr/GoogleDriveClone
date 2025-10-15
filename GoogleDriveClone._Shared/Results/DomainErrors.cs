using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDriveClone.SharedModels.Results;

// Enum для категоризації помилок
public enum ErrorType { NotFound, Validation, Unauthorized, Conflict }

// Record для опису помилки
public record Error(string Code, string Message, ErrorType Type);

// Статичний клас для зберігання всіх можливих помилок
public static class DomainErrors
{
    public static class User
    {
        public static Error InvalidCredentials { get; } = new(
            "User.InvalidCredentials",
            "Неправильний логін або пароль.",
            ErrorType.Unauthorized);

        public static Error EmailAlreadyExists { get; } = new(
            "User.EmailAlreadyExists",
            "Користувач з такою поштою вже існує.",
            ErrorType.Conflict);

        public static Error UsernameAlreadyExists { get; } = new(
            "User.UsernameAlreadyExists",
            "Користувач з таким іменем вже існує.",
            ErrorType.Conflict);

        public static Error NotFound { get; } = new(
            "User.NotFound",
            "Користувача не знайдено.",
            ErrorType.NotFound);

        public static Error InvalidEmail { get; } = new(
            "User.InvalidEmail",
            "Некоректний формат email.",
            ErrorType.Validation);

        public static Error WeakPassword { get; } = new(
            "User.WeakPassword",
            "Пароль не відповідає вимогам безпеки.",
            ErrorType.Validation);

        public static Error PasswordMismatch { get; } = new(
            "User.PasswordMismatch",
            "Паролі не збігаються.",
            ErrorType.Validation);

        public static Error RegistrationFailed { get; } = new(
            "User.RegistrationFailed",
            "Помилка реєстрації користувача.",
            ErrorType.Validation);
    }

    public static class File
    {
        public static Error NotFound { get; } = new(
            "File.NotFound",
            "Файл не знайдено.",
            ErrorType.NotFound);

        public static Error InvalidId { get; } = new(
            "File.InvalidId",
            "Некоректний формат ID файлу.",
            ErrorType.Validation);

        public static Error UploadFailed { get; } = new(
            "File.UploadFailed",
            "Не вдалося завантажити файл.",
            ErrorType.Validation);

        public static Error InvalidFileSize { get; } = new(
            "File.InvalidFileSize",
            "Розмір файлу перевищує дозволений ліміт.",
            ErrorType.Validation);

        public static Error InvalidFileType { get; } = new(
            "File.InvalidFileType",
            "Недозволений тип файлу.",
            ErrorType.Validation);
    }

    public static class Sync
    {
        public static Error CheckSupportFailed { get; } = new(
            "Sync.CheckSupportFailed",
            "Помилка перевірки підтримки синхронізації браузером.",
            ErrorType.Validation);

        public static Error NotSupported { get; } = new(
            "Sync.NotSupported",
            "Ваш браузер не підтримує синхронізацію папок. Використовуйте Chrome 86+ або Edge 86+.",
            ErrorType.Validation);

        public static Error FolderSelectionCancelled { get; } = new(
            "Sync.FolderSelectionCancelled",
            "Вибір папки скасовано користувачем.",
            ErrorType.Validation);

        public static Error FolderSelectionFailed { get; } = new(
            "Sync.FolderSelectionFailed",
            "Помилка вибору папки.",
            ErrorType.Validation);

        public static Error ReadFolderFailed { get; } = new(
            "Sync.ReadFolderFailed",
            "Помилка читання файлів з папки.",
            ErrorType.Validation);

        public static Error ServerFilesFailed { get; } = new(
            "Sync.ServerFilesFailed",
            "Не вдалося отримати список файлів з сервера.",
            ErrorType.Validation);

        public static Error CompareFailed { get; } = new(
            "Sync.CompareFailed",
            "Помилка порівняння локальних та серверних файлів.",
            ErrorType.Validation);

        public static Error ExecutionFailed { get; } = new(
            "Sync.ExecutionFailed",
            "Помилка виконання синхронізації.",
            ErrorType.Validation);

        public static Error CriticalError { get; } = new(
            "Sync.CriticalError",
            "Критична помилка синхронізації.",
            ErrorType.Validation);

        public static Error NoSupportedFiles { get; } = new(
            "Sync.NoSupportedFiles",
            "У вибраній папці немає підтримуваних файлів.",
            ErrorType.Validation);

        public static Error UserCancelled { get; } = new(
            "Sync.UserCancelled",
            "Синхронізація скасована користувачем.",
            ErrorType.Validation);
    }

    public static class General
    {
        public static Error ValidationFailed { get; } = new(
            "General.ValidationFailed",
            "Дані не пройшли валідацію.",
            ErrorType.Validation);

        public static Error UnexpectedError { get; } = new(
            "General.UnexpectedError",
            "Сталася непередбачена помилка.",
            ErrorType.Validation);
    }
}