# ND Crosshair

[English](README.md) · **Deutsch**

Kostenloses, quelloffenes Crosshair-Overlay für Windows. Es legt ein frei gestaltbares Fadenkreuz über jedes Spiel im Fenster- oder Fenster-Vollbildmodus.

## Funktionen

### Crosshair
- Form: Linien (einzeln schaltbar, also auch T-Form), Länge, Dicke, Abstand, Drehung (zum Beispiel 45° für ein X), Mittelpunkt (eckig oder rund), Ring
- Effekte: Umriss, weicher Schatten
- Farbmodi:
  - **Fest**: immer die gewählte Farbe
  - **Regenbogen**: fließender Farbwechsel mit einstellbarem Tempo
  - **Kontrast**: behält die eigene Farbe, solange sie sichtbar ist, und wechselt automatisch zu einer kontrastreichen Farbe, wenn sie im Hintergrund untergeht (Farbabstand in CIE-Lab mit Hysterese gegen Flackern)
- Kantenglättung per 4×4-Supersampling. Gerade Kanten bleiben pixelscharf.
- Live-Vorschau auf vier Hintergründen (Dunkel, Hell, Himmel, Wald)

### Presets
- Beliebig viele Presets mit Vorschaubild
- Share-Codes (`NDX3-...`) zum Teilen und Importieren, ältere `NDX1-` und `NDX2-`-Codes werden weiterhin gelesen

### Overlay
- Monitorauswahl und Versatz in Pixeln
- **Nur über dem Spiel**: blendet das Overlay aus, wenn ein anderes Fenster im Vordergrund ist. Erkannt wird ausschließlich der Fenstertitel, ohne Zugriff auf den Spielprozess. "Fenster erfassen" übernimmt den Titel des Spiels nach drei Sekunden automatisch.
- **Streamer-Modus**: Das Crosshair erscheint nicht in OBS, Discord oder Screenshots (`SetWindowDisplayAffinity`, ab Windows 10 2004)

### Hotkeys
- Overlay ein/aus (Standard: F8), nächstes Preset, vorheriges Preset
- Optionaler Hinweis beim Preset-Wechsel

### System
- Mit Windows starten, im Hintergrund starten, in den Infobereich minimieren, beim Schließen weiterlaufen
- Sprache: Systemsprache, Deutsch oder Englisch, live umschaltbar
- Sicherung exportieren und wiederherstellen, Konfigurationsordner öffnen, auf Standard zurücksetzen

### Oberfläche
- Eigenes dunkles Design mit eigener Titelleiste, Seitenleiste und Kategorien
- Eigene Dialoge, eigener Farbwähler, eigene Benachrichtigungen und eigenes Tray-Menü
- Ausnahme: Für Export und Import werden die Datei-Dialoge von Windows verwendet

## Download

Fertige Versionen gibt es unter [Releases](https://github.com/nowaaak/nd-crosshair/releases):

- `NdCrosshair-<Version>-win-x64-portable.zip`: läuft ohne Installation, enthält .NET (etwa 70 MB)
- `NdCrosshair-<Version>-win-x64-requires-dotnet8.zip`: deutlich kleiner, braucht die [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

ZIP entpacken, `NdCrosshair.exe` an einen festen Ort legen und starten. Die EXE ist nicht signiert, deshalb kann Windows SmartScreen beim ersten Start warnen ("Weitere Informationen", dann "Trotzdem ausführen").

## Voraussetzungen

- Windows 10 oder 11
- .NET 8 Desktop Runtime (zum Ausführen) bzw. .NET 8 SDK (zum Bauen)

## Selbst bauen

```powershell
dotnet test
dotnet publish src/NdCrosshair.App -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
.\publish\NdCrosshair.exe
```

Beim ersten Start öffnen sich die Einstellungen. Danach startet die App standardmäßig im Hintergrund. Ein Doppelklick auf das Tray-Icon oder ein erneuter Start der EXE öffnet die Einstellungen. Beenden über das Tray-Menü.

Autostart trägt den Pfad der gerade laufenden EXE in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` ein. Autostart deshalb aus der veröffentlichten EXE heraus aktivieren, nicht aus einem Debug-Build.

Die Konfiguration liegt unter `%APPDATA%\NdCrosshair\config.json`. Eine beschädigte Datei wird beim Start als `config.json.corrupt-<Zeitstempel>` gesichert und durch Standardwerte ersetzt.

## Release erstellen

Ein Tag wie `v1.0.0` startet den Release-Workflow. Er testet, baut beide Varianten, erstellt die ZIP-Dateien mit SHA-256-Prüfsummen und veröffentlicht ein GitHub-Release. Manuell gestartet erzeugt derselbe Workflow nur Test-Builds als Artefakte, ohne Release.

## Fortnite und Anti-Cheat

- Anzeigemodus in Fortnite auf **Fenster-Vollbild** stellen. Im exklusiven Vollbild zeichnet Windows keine fremden Fenster über das Spiel.
- Das Overlay greift nicht auf den Fortnite-Prozess zu. Es liest keinen Speicher, injiziert nichts, nutzt keine Tastatur- oder Maus-Hooks und braucht weder Netzwerk noch Admin-Rechte.
- Einzige Ausnahme ist der Farbmodus **Kontrast**: Er liest etwa zehnmal pro Sekunde einen kleinen Rahmen des Bildschirms rund um das Crosshair (`BitBlt`). Das Spiel wird dabei nicht angefasst. Ob Anti-Cheat-Systeme das dauerhaft tolerieren, ist nicht garantiert. Der Modus ist standardmäßig aus.
- Epic hat Crosshair-Overlays bisher weder ausdrücklich erlaubt noch verboten. Eine Garantie gegen künftige Regeländerungen gibt es nicht, auch nicht bei gekauften Tools.
- Hotkeys werden systemweit reserviert. Das Spiel erhält belegte Tasten nicht mehr.

## Aufbau

- `src/NdCrosshair.Core`: Einstellungen, Renderer (Rastern und Einfärben getrennt), Farbmathematik, adaptive Farbwahl, Share-Code, Konfiguration mit Export/Import, Übersetzungen. Ohne Windows-Abhängigkeit, getestet.
- `src/NdCrosshair.App`: WPF-Oberfläche (`Views`, `Controls`, `Theme.xaml`), Overlay als natives Win32-Layered-Window, `OverlayController` für Farbmodi und Sichtbarkeit, Dienste für Bildschirm-Sampling, Vordergrundfenster, Autostart und Benachrichtigungen.
- `tests/NdCrosshair.Core.Tests`: xUnit-Tests, inklusive Prüfung, dass jeder in der App verwendete Übersetzungsschlüssel in beiden Sprachen existiert.

## Mögliche Erweiterungen

- Xbox-Game-Bar-Widget für exklusives Vollbild
- Ausblenden bei gehaltener rechter Maustaste (ADS)
- Eigene PNG-Bilder als Crosshair

## Lizenz

[MIT](LICENSE)
