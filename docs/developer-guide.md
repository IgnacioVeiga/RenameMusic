# RenameMusic Developer Guide

## 1. Project goals
RenameMusic is a desktop app that renames music files based on a user-defined metadata template.

Current core goals:
- Handle very large lists of audio files.
- Persist session state in SQLite so users can recover work after closing the app.
- Keep a clear separation between UI and logic using MVVM.

## 2. Tech stack
- .NET 8
- WPF
- EF Core 8 with SQLite
- CommunityToolkit.Mvvm
- TagLib# (taglib-sharp-netstandard2.0)

## 3. Runtime model
The app now boots from `App.xaml.cs` and explicitly creates:
- `RenameMusic.Views.MainWindow`
- `RenameMusic.ViewModels.MainWindowViewModel`

The old root-level `MainWindow` was removed to avoid startup confusion.

## 4. MVVM architecture

### 4.1 View
`RenameMusic/Views/MainWindow.xaml`
- DataGrid and Menu bind to ViewModel commands and observable collections.
- Main code-behind is intentionally thin.

### 4.2 ViewModel
`RenameMusic/ViewModels/MainWindowViewModel.cs`
- Coordinates startup, session loading, AddFile, AddFolder, rule changes, and batch rename.
- Owns UI state (`ToRenameItems`, `DoNotRenameItems`, `FolderItems`, status text, selected cover).
- Uses async commands via `CommunityToolkit.Mvvm`.

### 4.3 Services
- `SessionService`: persistence and ingestion pipeline.
- `TemplateRuleService`: metadata template parsing and rename eligibility.
- `RenameExecutionService`: physical rename process and conflict handling.
- `DialogService`: message boxes and modal windows.
- `FilePickerService`: file and folder selection.

## 5. Persistence model

### 5.1 Tables
Primary tables:
- `SessionAudios` (`SessionAudioEntity`)
- `SessionFolders` (`SessionFolderEntity`)

### 5.2 Indexes
Defined in `MyContext.OnModelCreating`:
- Unique: `SessionAudios.FullPath`
- Non-unique: `SessionAudios.FolderPath`
- Non-unique: `SessionAudios.FileNameWithoutExtension`
- Non-unique: `SessionAudios.CanRename`
- Unique: `SessionFolders.FolderPath`

## 6. Rename rules

Current behavior:
- Template must contain at least one supported tag.
- Tags used in the template define required metadata for strict mode.
- Missing tag strategy is configurable:
  - Strict
  - Use placeholder (`Unknown` or localized equivalent)
- Repeated tags are allowed, but warned to the user.

Supported tags:
- `<TrackNum>`
- `<Title>`
- `<Album>`
- `<AlbumArtist>`
- `<Artist>`
- `<Year>`

## 7. Session behavior
- On startup, if a saved session exists, user is asked whether to load it.
- If user declines, app warns and deletes saved session.
- Missing files are moved to `Do Not Rename` with reason `File not found.`
- App shows an aggregated warning when missing files are detected.

## 8. Conflict behavior
- Existing `RepeatedFile` modal is still used.
- Batch rename supports apply-to-all decision for current run.
- Persistent default conflict policy is intentionally postponed.

Code marker:
- `TODO` in `RenameExecutionService.ResolveConflict` for persistent policy support.

## 9. Current scope and deferred work

Implemented now:
- AddFile
- AddFolder
- Load session data
- Template recalculation with confirmation
- Batch rename with conflict resolution
- Item-level actions from context menu:
  - play file
  - edit tags
  - move between `To Rename` and `Do Not Rename`
  - rename single file now
- Folder removal from session including subfolders
- Resilient folder scan that skips inaccessible subdirectories instead of aborting ingestion

Deferred intentionally:
- `DeleteFile` and `DeleteFolder` workflows (must be implemented safely)
- Persistent default conflict policy
- Additional UX redesign beyond current tab-based layout

## 10. Build and test notes
- Full WPF build must run on Windows with .NET Desktop SDK.
- Linux environments without `Microsoft.NET.Sdk.WindowsDesktop` cannot compile this project.
- Keep CI workflows on Windows runners for build validation.

## 11. Conventions for contributors
- Keep business logic in services and ViewModels, not in code-behind.
- Keep filesystem operations centralized in rename/session services.
- Use async APIs in long operations.
- Add clear `TODO` comments in English for deferred behavior.
- Keep `RenameMusic_v1` removed and do not reintroduce legacy project copies.
