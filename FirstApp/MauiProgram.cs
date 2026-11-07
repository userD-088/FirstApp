using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace FirstApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if ANDROID
        // Borderless entry: the surrounding Border draws the frame (no Material underline)
        EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
