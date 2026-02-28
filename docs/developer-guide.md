# RenameMusic Developer Guide

## 1. Purpose
RenameMusic renames music files using metadata-based templates.

## 2. Solution overview
The solution now has three projects:

1. `RenameMusic` (WPF UI)
2. `RenameMusic.Core` (business logic)
3. `RenameMusic.Tests` (unit tests)

See [architecture.md](./architecture.md) for details.

## 3. Key technologies
- .NET 8
- WPF
- EF Core 8 + SQLite
- CommunityToolkit.Mvvm
- TagLib#
- xUnit

## 4. Main user flows
- Add files
- Add folders
- Load previous session
- Apply template rule
- Rename all or single item
- Resolve filename conflicts
- Move items between rename lists
- Delete files/folders safely (Recycle Bin)

## 5. Rename rule behavior
- Template must contain at least one supported tag.
- Required metadata is controlled by `MinTagsRequiredIndex`:
  - `None required`
  - `Only marked ones`
  - `All mentioned`
- Missing tags strategy:
  - `Strict`
  - `Use placeholder` (`Unknown`/localized)

## 6. Persistence behavior
- Session data is persisted in SQLite.
- Startup asks whether to restore previous session.
- Missing files are moved to `Do Not Rename` with reason.
- Ingestion writes are batched for large lists.

## 7. Conflict behavior
- Conflict modal is still available (`RepeatedFile`).
- Default conflict policy is configurable:
  - Ask
  - Replace
  - Skip
  - RenameWithNumber

## 8. Build and test
- WPF build requires Windows + .NET Desktop SDK.
- Core and tests can run cross-platform.

Commands:

```bash
dotnet restore RenameMusic.sln
dotnet test RenameMusic.Tests/RenameMusic.Tests.csproj -c Release
```

## 9. Documentation map
- Architecture: [architecture.md](./architecture.md)
- Testing: [testing-guide.md](./testing-guide.md)
- Template token replacement strategy: [template-token-replacement.md](./template-token-replacement.md)
- Spanish developer guide: [developer-guide.es.md](./developer-guide.es.md)

## 10. Conventions
- Keep business logic in `RenameMusic.Core`.
- Keep WPF code-behind minimal.
- Prefer service interfaces for boundaries.
- Add tests for new business rules.
- Use English `TODO` comments when needed.
