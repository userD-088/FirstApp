namespace FirstApp.Core;

/// <summary>Typed wrapper around <see cref="Preferences"/>. Keys of v1.0 are kept for compatibility.</summary>
public static class AppSettings
{
    private static IPreferences P => Preferences.Default;

    public static string Language { get => P.Get("pref_lang", Loc.SystemDefault()); set => P.Set("pref_lang", value); }
    public static int Theme { get => P.Get("pref_theme", 0); set => P.Set("pref_theme", value); }
    public static int Series { get => P.Get("pref_series", 12); set => P.Set("pref_series", value); }

    public static bool Animations { get => P.Get("pref_anim", true); set => P.Set("pref_anim", value); }
    public static bool Haptics { get => P.Get("pref_haptics", true); set => P.Set("pref_haptics", value); }
    public static bool Fullscreen { get => P.Get("pref_fullscreen", true); set => P.Set("pref_fullscreen", value); }
    public static bool RememberState { get => P.Get("pref_remember", true); set => P.Set("pref_remember", value); }
    public static bool ShowRange { get => P.Get("pref_range", true); set => P.Set("pref_range", value); }

    // ── Last state (only used when RememberState is on) ──────────────
    public static string LastState { get => P.Get("state_v2", ""); set => P.Set("state_v2", value); }

    /// <summary>Restores all user preferences except the language.</summary>
    public static void ResetToDefaults()
    {
        foreach (var key in new[] { "pref_theme", "pref_series", "pref_anim", "pref_haptics",
                                    "pref_fullscreen", "pref_remember", "pref_range", "state_v2" })
            P.Remove(key);
    }
}
