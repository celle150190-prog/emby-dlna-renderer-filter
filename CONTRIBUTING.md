# Contributing

Contributions are welcome through GitHub Pull Requests.

## Rules

- Do not commit directly to the maintainer's release branch.
- Keep changes focused and explain why they are needed.
- Do not include credentials, API keys, tokens, private URLs, personal data or production secrets.
- Security-sensitive changes should be discussed before public disclosure.
- Pull requests must pass the GitHub Actions build before merge.
- Changes to the runtime PlayTo hook, plugin permissions, release workflow or dependencies require maintainer review.

## Build

```bash
dotnet restore Emby.DlnaRendererFilter.csproj
dotnet build Emby.DlnaRendererFilter.csproj -c Release --no-restore
```

The project targets `netstandard2.0`.

## Pull requests

Please include:

- Emby Server version used for testing
- operating system / platform
- what behavior changed
- relevant log output when fixing a bug

By contributing, you agree that your contribution is provided under the repository's MIT license.
