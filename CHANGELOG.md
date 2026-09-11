# Changelog

## 0.1.4 - 2026-09-11

- Added plugin thumbnail support.
- GitHub Actions artifact names now use the plugin version automatically.
- Confirmed working on Emby Server 4.9.5.0.

## 0.1.3 - 2026-09-11

- Reworked the configuration page controller to use Emby's `BaseView` lifecycle.
- Fixed the `lifeCycleAbortSignal` configuration-page error on Emby 4.9.5.0.

## 0.1.2 - 2026-09-11

- Registered the embedded JavaScript controller as an Emby plugin page resource.
- Updated configuration-page structure.

## 0.1.1 - 2026-09-11

- Split configuration logic into an embedded JavaScript controller.

## 0.1.0 - 2026-09-11

- Initial proof of concept.
- DLNA renderer discovery inventory.
- Per-UUID filtering of Emby's PlayTo renderer discovery.
- Fail-open runtime hook with restoration on shutdown.
