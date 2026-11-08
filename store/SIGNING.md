# Signieren für Google Play

Der Play Store nimmt nur Apps an, die mit einem **Schlüssel (Keystore)** unterschrieben sind.
Jedes spätere Update muss mit demselben Schlüssel signiert sein. Du erstellst ihn **einmal** auf deinem PC,
sicherst ihn und hinterlegst ihn verschlüsselt bei GitHub. Danach baut GitHub bei jedem Push automatisch
eine fertig signierte `.aab`.

> ⚠️ Der früher ins Repo hochgeladene `resistor_calculator.keystore` ist öffentlich geworden – **nicht verwenden**.

## 1. Keystore erstellen (Windows, einmalig)
Öffne **PowerShell** (Startmenü → „PowerShell“ oder in Visual Studio: Ansicht → Terminal).
Zuerst `keytool` suchen – das Java-Werkzeug, das Visual Studio für Android mitinstalliert (dauert bis zu 1 Minute):

```powershell
$keytool = (Get-ChildItem "C:\Program Files", "C:\Program Files (x86)", "$env:LOCALAPPDATA", "$env:ProgramData" -Filter keytool.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
$keytool
```

Es muss ein Pfad erscheinen (z. B. `…\openjdk\jdk-17…\bin\keytool.exe`). Erscheint nichts: Visual Studio →
Extras → Optionen → Xamarin/.NET MAUI → Android-Einstellungen → dort den Java-Pfad ablesen (oder das JDK
installieren lassen) und `$keytool = "<Pfad>\bin\keytool.exe"` setzen.

Dann den Keystore erstellen:

```powershell
& $keytool -genkeypair -v -storetype PKCS12 -keyalg RSA -keysize 4096 -validity 10000 -alias upload -keystore "$env:USERPROFILE\Documents\resistorcalc-upload.keystore"
```

Es folgen Fragen:
- **Passwort** (zweimal): ein langes, eigenes Passwort, z. B. vier zufällige Wörter. Gut merken!
- **Vor- und Nachname**: `Daniel Horst`
- Organisation, Ort, Bundesland: einfach **Enter** drücken
- **Ländercode**: `DE`
- „Ist CN=Daniel Horst … richtig?“ → `ja` (bzw. `yes`)

Beim Passwort erscheinen beim Tippen keine Zeichen – das ist normal.

## 2. Sichern (wichtig!)
Die Datei `Dokumente\resistorcalc-upload.keystore` **und** das Passwort an mindestens zwei Orten sichern,
z. B. Passwort-Manager + USB-Stick. **Niemals ins Git-Repo legen** (die `.gitignore` blockiert das jetzt).

## 3. Bei GitHub hinterlegen
Den Keystore als Text in die Zwischenablage kopieren:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("$env:USERPROFILE\Documents\resistorcalc-upload.keystore")) | Set-Clipboard
```

Dann auf GitHub: **Repo → Settings → Secrets and variables → Actions → New repository secret**

| Name | Wert |
|---|---|
| `KEYSTORE_BASE64` | einfügen (Strg+V) |
| `KEYSTORE_PASSWORD` | dein Passwort aus Schritt 1 |
| `KEY_ALIAS` | `upload` |

Secrets sind verschlüsselt; niemand – auch du nicht – kann sie danach wieder anzeigen.

## 4. Build starten
**Actions → Android build → Run workflow**. In der Zusammenfassung steht dann
„✅ Signed with your upload key“. Unter *Artifacts* liegt `resistor-calculator-<Nr>` mit der
`…-Signed.aab` – diese Datei lädst du in der Play Console hoch.

## 5. Play App Signing
Beim ersten Upload bietet Google **„Play App Signing“** an → **annehmen**. Google verwahrt dann den
eigentlichen App-Schlüssel; dein Keystore ist nur noch der *Upload-Schlüssel*. Geht er verloren oder wird
er geklaut, kann Google ihn zurücksetzen (Play Console → Test und Release → App-Integrität).

## Versionsnummer
Jeder Upload braucht eine höhere interne Versionsnummer (*versionCode*). Der Workflow setzt sie automatisch
auf die Build-Nummer. Die sichtbare Version („1.0“) steht in `FirstApp/FirstApp.csproj` →
`ApplicationDisplayVersion`.
