using Android.App;
using Android.Content.PM;
using Android.OS;
using FirstApp.Core;
using FirstApp.UI;

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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (Window != null)
            SystemUi.Apply(Window, AppSettings.Fullscreen, Android.Graphics.Color.ParseColor("#0F172A"));
    }
}
