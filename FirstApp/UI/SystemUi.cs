#if ANDROID
using Microsoft.Maui.Platform;
#endif

namespace FirstApp.UI;

/// <summary>Status / navigation bar handling (fullscreen toggle).</summary>
public static partial class SystemUi
{
    public static void Apply(bool fullscreen, Color barColor, bool lightBars = false)
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity?.Window is not { } window) return;
        Apply(window, fullscreen, barColor.ToPlatform(), lightBars);
#endif
    }

#if ANDROID
    public static void Apply(Android.Views.Window window, bool fullscreen, Android.Graphics.Color barColor, bool lightBars = false)
    {
        AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, !fullscreen);
        var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller == null) return;

        if (fullscreen)
        {
            controller.Hide(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
            controller.SystemBarsBehavior = AndroidX.Core.View.WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }
        else
        {
            controller.Show(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
#pragma warning disable CA1422 // still the way to colour bars below API 35
            window.SetStatusBarColor(barColor);
            window.SetNavigationBarColor(barColor);
#pragma warning restore CA1422
            controller.AppearanceLightStatusBars = lightBars;
            controller.AppearanceLightNavigationBars = lightBars;
        }
    }
#endif
}
