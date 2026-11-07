# Resistor Calculator

.NET MAUI Android app for 4-, 5- and 6-band resistor colour codes (60 languages).

## Features
- **Value → colour** and **colour → value**; both modes share one resistor, so switching keeps the value
- Input like `4.7`, `470`, `4,7`, RKM notation `4k7`, `4R7`, `2M2`, plus a unit selector (Ω / kΩ / MΩ / GΩ)
- Result with tolerance, min/max range, TCR (6-band) and colour names – tap to copy
- IEC 60063 E-series (E3 … E192) with a "nearest standard value" hint
- Reference sheet: colour table, reading direction, E-series overview
- Settings: language, design (Default, Cyber, Plasma, AMOLED), animations, haptic feedback,
  fullscreen, tolerance range, remember last input, reset

## Project layout
| Path | Content |
|---|---|
| `FirstApp/Core/` | UI-free logic: `ResistorMath` (parse/encode/E-series), `ColorCodes`, `Loc` (localisation), `AppSettings` |
| `FirstApp/ResistorViewModel.cs` | App state, single source of truth |
| `FirstApp/MainPage.xaml(.cs)` | Main screen, bottom sheet host, animations |
| `FirstApp/UI/` | Theme palette, drawables, settings + reference sheets (built lazily in code) |
| `FirstApp/Resources/Localization/` | One flat JSON per language (embedded; missing keys fall back to English) |

## Build
```bash
dotnet workload install maui-android
dotnet publish FirstApp/FirstApp.csproj -f net9.0-android -c Release
```
Release builds are arm64-only, fully trimmed (R8 + IL trimming) and use profiled AOT for fast startup.
The GitHub Actions workflow builds the `.aab`/`.apk` on every push and reports the package size.

## Adding a language
1. Copy `Resources/Localization/en.json` to `<code>.json` and translate it.
2. Add `(code, flag emoji, label)` to `Loc.PrimaryLanguages` or `Loc.SecondaryLanguages` (`Core/Loc.cs`).
