# Emby Community beta-test post

## Suggested title

**[Plugin / Beta] DLNA Renderer Filter – hide selected DLNA devices from “Play on”**

## Suggested post

Hi everyone,

I would like to introduce **DLNA Renderer Filter**, a small Emby Server plugin for users who have DLNA/UPnP renderers that they do not want to see in Emby's **Play on** device list.

### What it does

The plugin detects the DLNA/UPnP `MediaRenderer` devices discovered by Emby Server and lists them on its settings page. Each renderer can then be enabled or hidden individually.

A hidden renderer is filtered only from Emby's DLNA PlayTo discovery path. The plugin does not change the device itself, does not add firewall rules and does not patch `Emby.Dlna.dll`.

New renderers are visible by default.

### Important limitation

This plugin filters **DLNA/UPnP renderers only**. Chromecast targets and active Emby client sessions are discovered through different paths and are therefore not listed or filtered by this plugin.

### Compatibility / current testing

Current release: **v0.1.4**

Tested successfully with:

- Emby Server 4.9.5.0
- Synology DSM 7.2
- Denon AVR-X4400H
- Sony soundbar
- custom DLNA-to-HEOS bridge renderer

The plugin targets `netstandard2.0`.

### Technical note

Emby's public device discovery API does not expose a per-renderer ignore hook. The plugin therefore uses reflection to replace only the `DeviceDiscovered` callback belonging to `Emby.Dlna.PlayTo.PlayToManager` with a proxy.

The implementation is deliberately fail-open: if a future Emby release changes that internal handler and the plugin cannot locate it, nothing is blocked and Emby continues normally.

### Download / source

GitHub repository:

https://github.com/celle150190-prog/emby-dlna-renderer-filter

Latest release:

https://github.com/celle150190-prog/emby-dlna-renderer-filter/releases/latest

The release contains `Emby.DlnaRendererFilter.dll`.

### Feedback requested

I would especially appreciate tests on:

- Windows
- Linux
- Docker
- other NAS platforms
- other Emby Server 4.9.x versions

If something does not work, please include the Emby Server version, platform and any log lines containing `DLNA Renderer Filter`.

The project is open source under the MIT license.

Thanks for testing!
