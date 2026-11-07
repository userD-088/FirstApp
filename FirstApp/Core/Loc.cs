using System.Globalization;
using System.Text.Json;

namespace FirstApp.Core;

/// <summary>
/// Lightweight localisation: flat JSON files embedded as resources ("loc.{code}.json"),
/// parsed with <see cref="Utf8JsonReader"/> – no reflection, so it survives full trimming.
/// Missing keys fall back to English, then to the key itself.
/// </summary>
public static class Loc
{
    /// <summary>(code, flag emoji, short label). Empty flag → image "flag_{code}.png".</summary>
    public static readonly (string Code, string Flag, string Short)[] PrimaryLanguages =
    {
        ("en","🇬🇧","EN"), ("de","🇩🇪","DE"), ("es","🇪🇸","ES"), ("ru","🇷🇺","RU"), ("fr","🇫🇷","FR"),
        ("pt","🇵🇹","PT"), ("ja","🇯🇵","JA"), ("zh","🇨🇳","ZH"),
    };

    public static readonly (string Code, string Flag, string Short)[] SecondaryLanguages =
    {
        ("af","🇿🇦","AF"),("am","🇪🇹","AM"),("ar","🇸🇦","AR"),("az","🇦🇿","AZ"),
        ("bg","🇧🇬","BG"),("bn","🇧🇩","BN"),("cs","🇨🇿","CS"),("da","🇩🇰","DA"),
        ("el","🇬🇷","EL"),("et","🇪🇪","ET"),("fa","🇮🇷","FA"),("fi","🇫🇮","FI"),
        ("gu","🇮🇳","GU"),("ha","🇳🇬","HA"),("he","🇮🇱","HE"),("hi","🇮🇳","HI"),
        ("hr","🇭🇷","HR"),("hu","🇭🇺","HU"),("hy","🇦🇲","HY"),("id","🇮🇩","ID"),
        ("is","🇮🇸","IS"),("it","🇮🇹","IT"),("jv","🇮🇩","JV"),("ka","🇬🇪","KA"),
        ("kn","🇮🇳","KN"),("ko","🇰🇷","KO"),("ku","","KU"),("la","🇻🇦","LA"),
        ("lt","🇱🇹","LT"),("lv","🇱🇻","LV"),("ml","🇮🇳","ML"),("mr","🇮🇳","MR"),
        ("ms","🇲🇾","MS"),("nl","🇳🇱","NL"),("no","🇳🇴","NO"),("pa","🇮🇳","PA"),
        ("pl","🇵🇱","PL"),("ro","🇷🇴","RO"),("sk","🇸🇰","SK"),("sl","🇸🇮","SL"),
        ("sq","🇦🇱","SQ"),("sr","🇷🇸","SR"),("sv","🇸🇪","SV"),("sw","🇰🇪","SW"),
        ("ta","🇮🇳","TA"),("te","🇮🇳","TE"),("th","🇹🇭","TH"),("tl","🇵🇭","TL"),
        ("tr","🇹🇷","TR"),("uk","🇺🇦","UK"),("ur","🇵🇰","UR"),("vi","🇻🇳","VI"),
    };

    private static readonly Dictionary<string, Dictionary<string, string>> _cache = new();
    private static Dictionary<string, string> _current = new();
    private static Dictionary<string, string>? _english;

    public static string Language { get; private set; } = "en";

    public static bool IsSupported(string code)
        => Array.Exists(PrimaryLanguages, l => l.Code == code) || Array.Exists(SecondaryLanguages, l => l.Code == code);

    public static string SystemDefault()
    {
        // .NET maps "nb"/"nn" to Norwegian – the app uses "no"
        string sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (sys is "nb" or "nn") sys = "no";
        return IsSupported(sys) ? sys : "en";
    }

    public static void SetLanguage(string code)
    {
        if (!IsSupported(code)) code = "en";
        Language = code;
        _current = Load(code);
    }

    public static string T(string key)
    {
        if (_current.TryGetValue(key, out var v)) return v;
        _english ??= Load("en");
        return _english.TryGetValue(key, out var en) ? en : key;
    }

    /// <summary>Pre-loads translations on a background thread (makes switching instant).</summary>
    public static void Preload(string code) => Load(code);

    private static Dictionary<string, string> Load(string code)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(code, out var cached)) return cached;
            var dict = new Dictionary<string, string>(96);
            try
            {
                using var stream = typeof(Loc).Assembly.GetManifestResourceStream($"loc.{code}.json");
                if (stream != null)
                {
                    var bytes = new byte[stream.Length];
                    stream.ReadExactly(bytes);
                    Parse(bytes, dict);
                }
            }
            catch
            {
                // Broken file → keep whatever was parsed; English fallback covers the rest
            }
            _cache[code] = dict;
            return dict;
        }
    }

    private static void Parse(ReadOnlySpan<byte> json, Dictionary<string, string> into)
    {
        if (json.Length >= 3 && json[0] == 0xEF && json[1] == 0xBB && json[2] == 0xBF) json = json[3..];
        var reader = new Utf8JsonReader(json, new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
        string? key = null;
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    key = reader.GetString();
                    break;
                case JsonTokenType.String when key != null:
                    into[key] = reader.GetString() ?? "";
                    key = null;
                    break;
            }
        }
    }
}
