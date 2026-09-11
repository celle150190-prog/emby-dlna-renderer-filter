# Emby DLNA Renderer Filter

Emby Server plugin that detects UPnP/DLNA `MediaRenderer` devices and lets you hide selected DLNA renderers from Emby's **Play on / Wiedergabe auf …** list.

## What it does

- Detects DLNA/UPnP `MediaRenderer` devices seen by Emby Server.
- Shows friendly name, manufacturer, model, IP address and UUID in the plugin settings page.
- Lets you hide individual DLNA renderers from Emby's PlayTo device list.
- New renderers remain visible by default.
- Does not require firewall rules.
- Does not modify `Emby.Dlna.dll`.
- Fails open: if an Emby update changes the internal PlayTo handler and the hook cannot be installed, the plugin blocks nothing.

## What it does not filter

The plugin only filters **server-side DLNA/UPnP renderers**.

It does **not** filter:

- Chromecast / Google Cast targets
- active Emby client sessions such as Emby for LG, Android, etc.
- other device types that do not pass through Emby's DLNA PlayTo discovery path

That is why Emby's **Play on** list can contain more devices than the plugin settings page.

## Compatibility

Tested with:

- Emby Server 4.9.5.0
- Synology DSM 7.2

The project targets `netstandard2.0` and uses `MediaBrowser.Server.Core 4.9.1.90`.

Other Emby versions may work, but have not yet been validated. Because the filter hooks an internal Emby PlayTo callback by reflection, an Emby update can require a plugin update.

## Installation

1. Download `Emby.DlnaRendererFilter.dll` from the latest GitHub Release.
2. Stop Emby Server.
3. Copy the DLL into the Emby Server plugin directory.
4. Start Emby Server.
5. Open **Dashboard → Plugins → DLNA Renderer Filter**.
6. Leave **Enable filter** enabled and disable any DLNA renderer that should no longer appear in **Play on**.
7. Save the configuration.
8. Restart Emby once if the renderer had already been created as an active PlayTo session.

### Synology DSM example

On the tested Synology package installation the source plugin directory is:

```text
/volume1/@appstore/EmbyServer/system/plugins/
```

Example:

```bash
cp Emby.DlnaRendererFilter.dll /volume1/@appstore/EmbyServer/system/plugins/
chmod 644 /volume1/@appstore/EmbyServer/system/plugins/Emby.DlnaRendererFilter.dll
synopkg restart EmbyServer
```

Emby may copy the plugin into its writable runtime plugin directory during startup. This is normal.

## Verification

Successful startup:

```text
DLNA Renderer Filter: gestartet.
DLNA Renderer Filter: PlayTo-Filter aktiv. Event-Feld: ...
```

When a renderer is filtered:

```text
DLNA Renderer Filter: PlayTo-Renderer blockiert: <uuid> (<location>)
```

## How it works

Emby's public `IDeviceDiscovery` API exposes discovery events but no public per-renderer ignore hook.

The plugin therefore locates only the `DeviceDiscovered` callback belonging to `Emby.Dlna.PlayTo.PlayToManager` and replaces that delegate at runtime with a small proxy. Other SSDP subscribers are left untouched.

The original handler is called for visible devices and skipped only for UUIDs selected as hidden in the plugin configuration. On shutdown the plugin attempts to restore the original handler.

## Build

```bash
dotnet restore Emby.DlnaRendererFilter.csproj
dotnet build Emby.DlnaRendererFilter.csproj -c Release --no-restore
```

Output:

```text
bin/Release/netstandard2.0/Emby.DlnaRendererFilter.dll
```

GitHub Actions also builds the plugin automatically and publishes a versioned artifact.

## Support / beta testing

If you test the plugin on another Emby Server version or operating system, please report:

- Emby Server version
- operating system / platform
- whether the settings page opens
- whether the expected DLNA renderer appears in the plugin
- whether hiding it removes it from **Play on**
- relevant `DLNA Renderer Filter` log lines if it fails

## License

MIT. See [LICENSE](LICENSE).

## Current release

**v0.1.4**

- working configuration UI on Emby 4.9.5.0
- per-UUID DLNA renderer filtering
- plugin thumbnail
- versioned GitHub Actions artifacts
- tested successfully with a Denon AVR-X4400H, a custom HEOS bridge renderer and a Sony soundbar
