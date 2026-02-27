# RenameMusic (Beta)

<p align="center">
  <img src="./RenameMusic/Resources/Icons/icon.png" alt="RenameMusic icon" width="120" />
</p>

RenameMusic is a desktop app to rename music files using metadata templates.

Language: **English** / [Español](./README_es.md)

## What it does
- Reads audio metadata (`mp3`, `m4a`, `ogg`, `flac`)
- Builds target filenames from a template
- Splits items into `To Rename` and `Do Not Rename`
- Persists session data in SQLite
- Handles filename conflicts with user policy
- Supports single-item and batch rename

## Solution structure
- `RenameMusic` -> WPF app (UI)
- `RenameMusic.Core` -> domain and business logic
- `RenameMusic.Tests` -> unit tests

## Requirements
- Windows 10/11 recommended to run WPF app
- .NET SDK 8
- .NET Desktop Runtime 8 (for running app)

## Build and test
From repository root:

```bash
# Restore all projects
dotnet restore RenameMusic.sln

# Run unit tests (cross-platform)
dotnet test RenameMusic.Tests/RenameMusic.Tests.csproj -c Release
```

Build WPF app (Windows only):

```bash
dotnet build RenameMusic/RenameMusic.csproj -c Release
```

## CI
GitHub Actions validates:
- restore
- build
- unit tests

(Branches `master` and `main` are excluded by workflow config.)

Release workflow:
- pushing a tag like `v1.2.0` builds and tests the solution
- publishes `win-x86` and `win-x64` packages
- creates a GitHub Release with both `.zip` assets

## Documentation
- Developer guide (EN): [docs/developer-guide.md](./docs/developer-guide.md)
- Developer guide (ES): [docs/developer-guide.es.md](./docs/developer-guide.es.md)
- Architecture (EN): [docs/architecture.md](./docs/architecture.md)
- Architecture (ES): [docs/architecture.es.md](./docs/architecture.es.md)
- Testing guide (EN): [docs/testing-guide.md](./docs/testing-guide.md)
- Testing guide (ES): [docs/testing-guide.es.md](./docs/testing-guide.es.md)

## License
See [LICENSE.md](./LICENSE.md).
