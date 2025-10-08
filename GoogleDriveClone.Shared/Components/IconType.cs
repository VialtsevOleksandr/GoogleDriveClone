namespace GoogleDriveClone.Shared.Components;

/// <summary>
/// Enum для всіх доступних SVG іконок в Gaming Drive
/// </summary>
public enum IconType
{
    // Основні дії
    Upload,
    Download,
    Delete,
    View,
    Search,
    Sync,
    Edit,
    Save,

    // Навігація та UI
    Folder,
    Settings,
    SortUp,
    SortDown,
    Close,
    Menu,
    
    // Статуси
    Success,
    Error,
    Info,
    Loading,
    
    // Типи файлів
    ImageFile,
    CodeFile,
    DocumentFile,
    ArchiveFile,
    DefaultFile,
    
    // Авторизація
    User,
    Logout,
    Login,
    
    // Gaming специфічні
    Lightning,
    GameController,
    
    // Додаткові іконки для features
    Rocket,
    Lock,
    Target,
    Globe,
    Stats,
    Cloud
}