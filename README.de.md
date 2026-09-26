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
- Kantenglättung per 4×4-Supersampling. Gerade Kanten bleiben pixelscharf.
- Live-Vorschau auf vier Hintergründen (Dunkel, Hell, Himmel, Wald)
- Hinweis, wenn Punktgröße und Linienstärke unterschiedliche Parität haben und der Punkt deshalb einen halben Pixel versetzt sitzt

### Presets
- Beliebig viele Presets mit Vorschaubild, optional mit eigenem Hotkey
- Share-Codes (`NDX3-...`) zum Teilen und Importieren, ältere `NDX1-` und `NDX2-`-Codes werden weiterhin gelesen
- **Import von Spiel-Codes**: CS2-Codes (`CSGO-...`, aktuelles Pixel-Format seit dem Update im September 2026) und Valorant-Codes (`0;P;...`). Was sich nicht exakt abbilden lässt (zum Beispiel äußere Linien oder dynamische Streuung), meldet die App nach dem Import. CS2-Codes werden auf die Höhe des gewählten Monitors skaliert. Alte CS2-Codes im Einheiten-Format werden abgelehnt, weil CS2 sie selbst nicht mehr annimmt.

### Overlay
- Monitorauswahl und Versatz in Pixeln
- **Spiele**: Liste aus Prozessnamen (`cs2.exe`, exakter Treffer) oder Teilen des Fenstertitels. Jedem Spiel kann ein Preset zugeordnet werden, das beim Wechsel ins Spiel automatisch aktiv wird. "Fenster erfassen" übernimmt nach drei Sekunden den Prozess des Vordergrundfensters. Ältere Konfigurationen mit Fenstertiteln werden automatisch übernommen.
- **Nur über dem Spiel**: blendet das Overlay aus, wenn kein Spiel aus der Liste im Vordergrund ist
- **Beim Zielen ausblenden**: rechte, mittlere Maustaste, Maus 4 oder Maus 5, wahlweise halten oder umschalten
- **Streamer-Modus**: Das Crosshair erscheint nicht in OBS, Discord oder Screenshots (`SetWindowDisplayAffinity`, ab Windows 10 2004)

### Hotkeys
- Overlay ein/aus (Standard: F8), nächstes Preset, vorheriges Preset, ein Hotkey pro Preset
- Optional: Position mit Alt + Umschalt + Pfeiltasten verschieben, Alt + Umschalt + Pos1 setzt sie zurück
- Doppelt vergebene Tastenkombinationen werden erkannt und angezeigt
- Optionaler Hinweis beim Preset-Wechsel auf dem Monitor des Overlays

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

Die Konfiguration liegt unter `%APPDATA%\NdCrosshair\config.json`. Eine beschädigte Datei wird beim Start als `config.json.corrupt-<Zeitstempel>` gesichert und durch Standardwerte ersetzt. Ist die Datei gesperrt oder nicht lesbar, startet die App mit Standardwerten und speichert in dieser Sitzung nichts, damit die Datei unverändert bleibt.

## Release erstellen

Ein Tag wie `v1.0.0` startet den Release-Workflow. Er testet, baut beide Varianten, erstellt die ZIP-Dateien mit SHA-256-Prüfsummen und veröffentlicht ein GitHub-Release. Manuell gestartet erzeugt derselbe Workflow nur Test-Builds als Artefakte, ohne Release.

## Fortnite und Anti-Cheat

- Anzeigemodus in Fortnite auf **Fenster-Vollbild** stellen. Im exklusiven Vollbild zeichnet Windows keine fremden Fenster über das Spiel.
- Das Overlay greift nicht auf den Spielprozess zu. Es öffnet kein Handle auf das Spiel, liest keinen Speicher, injiziert nichts, nutzt keine Tastatur- oder Maus-Hooks und braucht weder Netzwerk noch Admin-Rechte.
- Zur Spielerkennung liest die App den Titel des Vordergrundfensters und den Prozessnamen aus der Windows-Prozessliste (`CreateToolhelp32Snapshot`), so wie der Task-Manager. Die Liste wird nur gelesen, wenn ein anderes Fenster in den Vordergrund kommt.
- "Beim Zielen ausblenden" fragt etwa hundertmal pro Sekunde den Zustand der gewählten Maustaste ab (`GetAsyncKeyState`). Das ist kein Hook und verändert keine Eingaben. Die Abfrage läuft nur, solange die Funktion aktiv und das Overlay sichtbar ist.
- Die App liest keine Pixel vom Bildschirm. Der frühere Farbmodus **Kontrast**, der den Bildschirm rund um das Crosshair abgetastet hat, wurde entfernt. Presets und Share-Codes, die ihn noch nutzen, fallen auf **Fest** zurück.
- Epic hat Crosshair-Overlays bisher weder ausdrücklich erlaubt noch verboten. Eine Garantie gegen künftige Regeländerungen gibt es nicht, auch nicht bei gekauften Tools.
- Hotkeys werden systemweit reserviert. Das Spiel erhält belegte Tasten nicht mehr.

## Aufbau

- `src/NdCrosshair.Core`: Einstellungen, Renderer (Rastern und Einfärben getrennt), Farbmathematik, Share-Code, Import von CS2- und Valorant-Codes, Spielregeln, Hotkey-Konflikte, Konfiguration mit Export/Import und Migration, Übersetzungen. Ohne Windows-Abhängigkeit, getestet.
- `src/NdCrosshair.App`: WPF-Oberfläche (`Views`, `Controls`, `Theme.xaml`), Overlay als natives Win32-Layered-Window, `OverlayController` für Farbmodi und Sichtbarkeit, Dienste für Vordergrundfenster, Maustastenstatus, Autostart und Benachrichtigungen.
- `tests/NdCrosshair.Core.Tests`: xUnit-Tests, inklusive Prüfung, dass jeder in der App verwendete Übersetzungsschlüssel in beiden Sprachen existiert.

## Mögliche Erweiterungen

- Xbox-Game-Bar-Widget für exklusives Vollbild
- Eigene PNG-Bilder als Crosshair
- Eigene Farbe und Deckkraft für Linien, Punkt und Ring, äußere Linien
- Schuss-Animation (Linien spreizen beim Feuern)

## Feedback

Fehler gefunden oder eine Idee? Eröffne ein [Issue](https://github.com/nowaaak/nd-crosshair/issues/new/choose). Pull Requests werden nicht angenommen, siehe [CONTRIBUTING](CONTRIBUTING.md).

## Lizenz

[MIT](LICENSE)
