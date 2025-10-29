# GoogleDriveClone

## 🚀 Overview
GoogleDriveClone is a comprehensive file management system that replicates the functionality of Google Drive. It allows users to upload, download, delete, and manage files, as well as synchronize files across devices. This project is designed for developers who want to build a robust file management system with advanced features.

## ✨ Features
- 📂 **File Management**: Upload, download, delete, and manage files.
- 🔒 **Authentication**: Secure user authentication with JWT tokens.
- 📈 **User Statistics**: Track file usage and storage.
- 🔄 **Folder Synchronization**: Sync files between folders.
- 🌐 **Cross-Platform**: Works on Windows and WEB.
- 🎨 **Gaming Theme**: Customizable UI with a gaming theme.

## 🛠️ Tech Stack
- **Programming Language**: C#
- **Frameworks**: ASP.NET Core, Entity Framework Core
- **Libraries**: Blazor, JWT, LocalStorage
- **Tools**: Docker, Visual Studio
- **Database**: SQL Server

## 📦 Installation

### Prerequisites
- .NET SDK 6.0 or later
- SQL Server (or any other supported database)

## 📁 Project Structure
```
GoogleDriveClone/
│
├── GoogleDriveClone.Api/
│   ├── Controllers/
│   ├── Data/
│   ├── Entities/
│   ├── Filters/
│   ├── Interfaces/
│   ├── Migrations/
│   ├── Middleware/
│   ├── Program.cs
│   ├── Properties/
│   ├── Repositories/
│   ├── Services/
│   └── wwwroot/
│
├── GoogleDriveClone._Shared/
│   ├── DTOs/
│   ├── Results/
│   ├── Services/
│   ├── Interfaces/
│   └── wwwroot/
│
├── GoogleDriveClone.Shared/
│   ├── Components/
│   ├── Extensions/
│   ├── Interfaces/
│   ├── Services/
│   ├── Utils/
│   └── wwwroot/
│
├── GoogleDriveClone.Shared.Auth/
│   ├── CustomAuthenticationStateProvider.cs
│   └── ...
│
├── GoogleDriveClone.Shared.Components/
│   ├── IconType.cs
│   └── ...
│
├── GoogleDriveClone.Shared.Extensions/
│   ├── ServiceCollectionExtensions.cs
│   └── ...
│
├── GoogleDriveClone.Shared.Interfaces/
│   ├── IAuthApiService.cs
│   ├── IAuthenticationService.cs
│   ├── ISyncService.cs
│   ├── IUserStatsService.cs
│   └── ...
│
├── GoogleDriveClone.Shared.Services/
│   ├── ApiConfigService.cs
│   ├── AuthApiService.cs
│   ├── AuthenticationService.cs
│   ├── FileDownloadService.cs
│   ├── FileManagerService.cs
│   ├── FileUploadService.cs
│   ├── MauiMenuService.cs
│   ├── NotificationService.cs
│   ├── PreferencesService.cs
│   ├── SyncService.cs
│   ├── UserStatsService.cs
│   └── ...
│
├── GoogleDriveClone.Shared.Utils/
│   ├── FileUtils.cs
│   └── ...
│
├── GoogleDriveClone.Shared/wwwroot/
│   ├── app.css
│   ├── auth.css
│   ├── files-manager.css
│   ├── gaming-home.css
│   └── svg-icons.css
│
└── .gitignore
```

## 🔧 Configuration
- **appsettings.json**: Configuration file for the application.
- **appsettings.Development.json**: Development-specific configuration.
- **appsettings.Production.json**: Production-specific configuration.

## 👥 Authors & Contributors
**Maintainers**: Alex Vialtsev
