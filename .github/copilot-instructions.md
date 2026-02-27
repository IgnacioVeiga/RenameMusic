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
6. Keep template dialog behavior in `ReplaceWithViewModel`, not in `ReplaceWith.xaml.cs`.

## Data model notes
- `SessionAudioEntity` and `SessionFolderEntity` are the active persistence model.
- Do not reintroduce DTO-based legacy storage paths.
- For large ingestions, prefer batched EF saves and avoid per-item DB writes.

## Rename rule behavior
1. Template must include at least one supported tag.
2. Required metadata depends on `MinTagsRequiredIndex` (`None`, `OnlyMarked`, `AllMentioned`).
3. Missing metadata strategy is user-configurable:
   - Strict
   - Placeholder (`Unknown` localized)
4. Repeated tags are allowed, but a warning should be shown.

## Session behavior
1. On startup, ask whether to load previous session.
2. If user declines, warn and clear saved session.
3. Missing files should be moved to `Do Not Rename` with reason.
4. Folder removal from session should also remove files in subfolders.

## Conflict behavior
1. Keep `RepeatedFile` modal flow.
2. Batch rename should support apply-to-all decision for the current run.
3. Support configurable default conflict policy (`Ask`, `Replace`, `Skip`, `RenameWithNumber`).

## MainWindow UX behavior
1. Keep item-level context actions available in tabs:
   - play file
   - edit metadata tags
   - move item between rename lists
   - rename a single file immediately
2. Keep ingestion resilient for inaccessible subfolders; skip and continue.

## Coding conventions
1. Use concise comments only when logic is not obvious.
2. Add `TODO` comments in English.
3. Prefer explicit, descriptive method names.
4. Avoid introducing hidden side effects in property setters.
5. Preserve localization usage where existing resource strings are available.
