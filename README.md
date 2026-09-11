# Emby DLNA Renderer Filter

Erweiterung für Emby Server, die erkannte UPnP/DLNA-MediaRenderer auflistet und ausgewählte Renderer aus Embys **„Wiedergabe auf …“** herausfiltert.

## Ziel

- Alle erkannten `MediaRenderer:1` erfassen.
- Friendly Name, Hersteller, Modell, IP, UUID und Description-URL speichern.
- Pro Gerät in der Plugin-Oberfläche festlegen, ob es in Emby PlayTo sichtbar sein soll.
- Neue Geräte standardmäßig sichtbar lassen.
- Keine Firewall-Regeln und keine Veränderung von `Emby.Dlna.dll`.

## Wichtiger technischer Hinweis

Emby stellt über die öffentliche `IDeviceDiscovery`-API nur Discovery-Events bereit, aber keinen offiziellen per-Gerät-Ignore-Hook. Deshalb ersetzt das Plugin zur Laufzeit per Reflection **nur** den `DeviceDiscovered`-Callback von `Emby.Dlna.PlayTo.PlayToManager` durch einen Proxy. Andere SSDP-Subscriber bleiben unverändert.

Das ist absichtlich fail-open: Kann der interne PlayTo-Handler nach einem Emby-Update nicht gefunden werden, wird nichts blockiert und Emby bleibt funktionsfähig. Im Log erscheint dann kein `PlayTo-Filter aktiv`.

## Build

Das Projekt verwendet `MediaBrowser.Server.Core 4.9.1.90` und `netstandard2.0`.

```bash
dotnet build Emby.DlnaRendererFilter.csproj -c Release
```

Ausgabe:

```text
bin/Release/netstandard2.0/Emby.DlnaRendererFilter.dll
```

Alternativ kann der mitgelieferte GitHub-Actions-Workflow verwendet werden.

## Installation auf DSM / Emby 4.9.x

1. Emby stoppen.
2. `Emby.DlnaRendererFilter.dll` in Embys `plugins`-Verzeichnis kopieren.
3. Emby starten.
4. Im Emby-Dashboard **Plugins → DLNA Renderer Filter** öffnen.
5. Renderer einmal neu erkennen lassen.
6. Unerwünschte Renderer deaktivieren und speichern.
7. Falls das Gerät bereits als PlayTo-Session vorhanden war, Emby einmal neu starten.

## Erwartete Logzeilen

Bei erfolgreicher Initialisierung:

```text
DLNA Renderer Filter: gestartet.
DLNA Renderer Filter: PlayTo-Filter aktiv. Event-Feld: ...
```

Beim Blockieren eines Geräts:

```text
DLNA Renderer Filter: PlayTo-Renderer blockiert: <uuid> (<location>)
```

## Version

0.1.0 – erster Proof-of-Concept für Emby 4.9.x.
