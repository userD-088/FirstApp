# Checkliste: Veröffentlichung im Google Play Store

## Einmalig vorbereiten
- [ ] **GitHub Pages aktivieren** (für die Datenschutzerklärung):
      Repo → Settings → Pages → *Source: Deploy from a branch* → Branch `master`, Ordner `/docs` → Save.
      Nach 1–2 Minuten erreichbar: https://userd-088.github.io/FirstApp/privacy.html
- [ ] **Keystore erstellen und bei GitHub hinterlegen** → [SIGNING.md](SIGNING.md)
- [ ] **Google-Play-Entwicklerkonto** anlegen: https://play.google.com/console (einmalig 25 US-$,
      Identitätsprüfung mit Ausweis). Als Kontotyp „Privatperson“.

## App anlegen (Play Console → App erstellen)
- [ ] Name: `Widerstandsrechner Farbcode`, Standardsprache: Deutsch (de-DE)
- [ ] App (nicht Spiel), Kostenlos
- [ ] Erklärungen bestätigen

## App-Inhalte (linkes Menü → Richtlinien → App-Inhalte)
Alle Antworten stehen in [data-safety.md](data-safety.md):
- [ ] Datenschutzerklärung (URL)
- [ ] App-Zugriff
- [ ] Werbung
- [ ] Einstufung des Inhalts
- [ ] Zielgruppe
- [ ] Datensicherheit
- [ ] Nachrichten-, Gesundheits-, Finanz- und Behörden-Erklärungen

## Store-Eintrag (Wachstum → Store-Präsenz → Haupteintrag)
- [ ] Texte aus [listing-de.md](listing-de.md); Übersetzung hinzufügen → Englisch aus [listing-en.md](listing-en.md)
- [ ] App-Symbol: [icon-512.png](icon-512.png)
- [ ] Vorstellungsgrafik: [feature-graphic.png](feature-graphic.png)
- [ ] **Mindestens 2 Smartphone-Screenshots** (Pflicht) – direkt am Handy aufnehmen (Power + Leiser).
      Gute Motive: Wert → Farbe, Farbe → Wert, Designauswahl, Referenztabelle, helles Design.
- [ ] Kategorie: Tools · Kontakt-E-Mail: neutron.mail216@proton.me · Website: https://userd-088.github.io/FirstApp/

## Testen und Veröffentlichen
- [ ] Neue **private** Entwicklerkonten müssen vor der Veröffentlichung einen **geschlossenen Test**
      durchführen: laut Google aktuell mindestens **12 Tester, 14 Tage lang** (in der Play Console unter
      „Dashboard“ steht, was für dein Konto gilt). Tester = Google-Konten von Freunden/Familie.
- [ ] Test und Release → Geschlossener Test → Track erstellen → `…-Signed.aab` aus GitHub Actions hochladen
- [ ] Beim ersten Upload **Play App Signing annehmen**
- [ ] Nach den 14 Tagen: **Produktionszugriff beantragen** → Release erstellen → zur Prüfung senden
      (Prüfung dauert meist 1–7 Tage)

## Bei jedem Update
1. Änderungen pushen → GitHub baut automatisch eine signierte `.aab` mit neuer Versionsnummer
2. Bei größeren Updates `ApplicationDisplayVersion` in `FirstApp/FirstApp.csproj` erhöhen (z. B. 1.1)
3. `.aab` in der Play Console hochladen, kurze Versionshinweise schreiben, veröffentlichen
