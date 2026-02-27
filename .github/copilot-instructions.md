# Copilot Instructions For RenameMusic

## Context
This repository contains a WPF desktop app for renaming music files from metadata templates.
Architecture target is MVVM with service-based business logic.

## Primary objectives
1. Keep AddFile, AddFolder, LoadData, and Rename workflows stable.
2. Support large lists using SQLite persistence.
3. Avoid moving business logic back into code-behind.

## Technical baseline
- .NET 8
- WPF
- EF Core 8 + SQLite
- CommunityToolkit.Mvvm
- TagLib#

## Architecture rules
1. Put UI interaction logic in ViewModels and services.
2. Keep code-behind minimal and UI-only.
3. Keep filesystem writes in dedicated services.
4. Keep async operations async end-to-end.
5. Use dependency-friendly abstractions for dialogs and pickers.

## Data model notes
- `SessionAudioEntity` and `SessionFolderEntity` are the active persistence model.
- `AudioDTO` and `FolderDTO` are legacy compatibility artifacts.
- Do not remove legacy tables unless migration and cleanup are explicitly planned.

## Rename rule behavior
1. Template must include at least one supported tag.
2. Used template tags define required metadata in strict mode.
3. Missing metadata strategy is user-configurable:
   - Strict
   - Placeholder (`Unknown` localized)
4. Repeated tags are allowed, but a warning should be shown.

## Session behavior
1. On startup, ask whether to load previous session.
2. If user declines, warn and clear saved session.
3. Missing files should be moved to `Do Not Rename` with reason.

## Conflict behavior
1. Keep `RepeatedFile` modal flow.
2. Batch rename should support apply-to-all decision for the current run.
3. Persistent default conflict policy is deferred and marked with `TODO`.

## Coding conventions
1. Use concise comments only when logic is not obvious.
2. Add `TODO` comments in English.
3. Prefer explicit, descriptive method names.
4. Avoid introducing hidden side effects in property setters.
5. Preserve localization usage where existing resource strings are available.
