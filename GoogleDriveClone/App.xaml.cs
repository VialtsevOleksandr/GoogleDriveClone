using GoogleDriveClone.Shared.Services;

namespace GoogleDriveClone;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var menuService = Handler?.MauiContext?.Services.GetService<MauiMenuService>();
        if (menuService == null)
        {
            throw new InvalidOperationException("MauiMenuService не зареєстровано");
        }
        
        return new Window(new NavigationPage(new MainPage(menuService))) { Title = "GoogleDriveClone" };
    }
}
