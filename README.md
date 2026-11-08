# Resistor Calculator

.NET MAUI Android app for 4-, 5- and 6-band resistor colour codes (60 languages).

## Features
- **Value → colour** and **colour → value**; both modes share one resistor, so switching keeps the value
- Input like `4.7`, `470`, `4,7`, RKM notation `4k7`, `4R7`, `2M2`, plus a unit selector (Ω / kΩ / MΩ / GΩ)
- Result with tolerance, min/max range, TCR (6-band) and colour names – tap to copy
- IEC 60063 E-series (E3 … E192) with a "nearest standard value" hint
- Reference sheet: colour table, reading direction, E-series overview
- Settings: 60 languages, 5 designs (Midnight, Light, Cyber, Plasma, AMOLED), animations,
  haptic feedback, fullscreen, tolerance range, remember last input, reset
- Fully offline: no internet permission, no ads, no tracking

## Project layout
| Path | Content |
|---|---|
| `FirstApp/Core/` | UI-free logic: `ResistorMath` (parse/encode/E-series), `ColorCodes`, `Loc` (localisation), `AppSettings` |
| `FirstApp/ResistorViewModel.cs` | App state, single source of truth |
| `FirstApp/MainPage.xaml(.cs)` | Main screen, bottom sheet host, animations |
| `FirstApp/UI/` | Theme palette, drawables, settings + reference sheets (built lazily in code) |
| `FirstApp/Resources/Localization/` | One flat JSON per language (embedded; missing keys fall back to English) |
| `docs/` | Website + privacy policy (GitHub Pages) |
| `store/` | Google Play: listing texts, graphics, data-safety answers, signing guide, release checklist |

## Build
**Requires Visual Studio 2026** (or the .NET 10 SDK) with the *.NET MAUI* workload.
Visual Studio 2022 cannot build .NET 10 projects (error: target platform identifier "android" not recognized).

```bash
dotnet workload install maui-android
dotnet publish FirstApp/FirstApp.csproj -f net10.0-android -c Release
```
Release builds are arm64-only, fully trimmed (R8 + IL trimming) and use profiled AOT for fast startup.
Targets Android 16 (API 36) as required by Google Play since August 2026.
The GitHub Actions workflow builds a signed `.aab` on every push once the signing secrets are set
(see [store/SIGNING.md](store/SIGNING.md)) and reports the package size.

## Publishing
See [store/CHECKLIST.md](store/CHECKLIST.md).

## Adding a language
1. Copy `Resources/Localization/en.json` to `<code>.json` and translate it.
2. Add `(code, flag emoji, label)` to `Loc.PrimaryLanguages` or `Loc.SecondaryLanguages` (`Core/Loc.cs`).
