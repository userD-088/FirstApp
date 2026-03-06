using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace FirstApp;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Window.SetDecorFitsSystemWindows(false);

        var controller = new WindowInsetsControllerCompat(
            Window,
            Window.DecorView);

        controller.Hide(WindowInsetsCompat.Type.SystemBars());
        controller.SystemBarsBehavior =
            WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
    }
}