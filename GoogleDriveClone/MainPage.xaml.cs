using Microsoft.AspNetCore.Components.WebView.Maui;
using GoogleDriveClone.Shared.Services;

namespace GoogleDriveClone
{
    public partial class MainPage : ContentPage
    {
        private readonly MauiMenuService _menuService;

        public MainPage(MauiMenuService menuService)
        {
            InitializeComponent();
            _menuService = menuService;
        }

        private void OnUploadFileClicked(object? sender, EventArgs e)
        {
            _menuService.TriggerUploadFile();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            Application.Current?.Quit();
        }

        private void OnSortAscendingClicked(object? sender, EventArgs e)
        {
            _menuService.TriggerSortAscending();
        }

        private void OnSortDescendingClicked(object? sender, EventArgs e)
        {
            _menuService.TriggerSortDescending();
        }

        private void OnShowDataClicked(object? sender, EventArgs e)
        {
            _menuService.TriggerShowData();
        }

        private async void OnAboutClicked(object? sender, EventArgs e)
        {
            await DisplayAlert(
                "Про програму",
                "Gaming Drive v1.0\n\n" +
                "🎮 Сучасне хмарне сховище для геймерів\n\n" +
                "✨ Особливості:\n" +
                "• Швидке завантаження файлів\n" +
                "• Синхронізація з папками\n" +
                "• Перегляд та редагування файлів\n" +
                "• Безпечне зберігання\n\n" +
                "Розроблено з використанням .NET MAUI та Blazor",
                "OK"
            );
        }
    }
}
