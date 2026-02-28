# RenameMusic Architecture

## Solution layout
The solution is split into three projects:

1. `RenameMusic` (WPF)
2. `RenameMusic.Core` (domain and application logic)
3. `RenameMusic.Tests` (unit tests)

## Project responsibilities

### `RenameMusic` (WPF)
- App startup and composition (`App.xaml.cs`)
- Windows and XAML views
- WPF-specific ViewModels
- UI services (dialogs, file pickers, theme, language)
- Localized resources (`.resx`)

### `RenameMusic.Core`
- Persistence model and EF Core context
- Rename rule evaluation
- Session ingestion and session persistence
- Rename execution and conflict handling
- Cross-project service contracts (`ISessionService`, `IDialogService`, etc.)
- Shared utility classes such as `FilenameFunctions`

### `RenameMusic.Tests`
- Unit tests for `RenameMusic.Core`
- No WPF dependency

## Runtime flow

1. WPF app starts in `App.xaml.cs`.
2. Services are instantiated.
3. `MainWindowViewModel` orchestrates user actions.
4. Core services handle ingestion, evaluation, persistence, and rename execution.
5. WPF project handles only UI interaction and presentation concerns.

## Session model
- `Do Not Rename` reasons are stored as stable codes (`CODE|detail`) and translated in UI.
- Theme and language settings are normalized to supported values (`Dark`/`Light`, `en`/`es`) for robust startup.

## Why this split
- Better maintainability and separation of concerns
- Core logic is testable without Windows Desktop SDK
- Faster feedback loop using unit tests
- Easier future migration to other frontends if needed
