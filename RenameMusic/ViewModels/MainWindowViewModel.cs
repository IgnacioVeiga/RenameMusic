using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RenameMusic.Models;
using RenameMusic.Properties;
using RenameMusic.Resources.Languages;
using RenameMusic.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RenameMusic.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly SessionService _sessionService;
        private readonly TemplateRuleService _templateRuleService;
        private readonly RenameExecutionService _renameExecutionService;
        private readonly IFilePickerService _filePickerService;
        private readonly IDialogService _dialogService;

        public MainWindowViewModel(
            SessionService sessionService,
            TemplateRuleService templateRuleService,
            RenameExecutionService renameExecutionService,
            IFilePickerService filePickerService,
            IDialogService dialogService)
        {
            _sessionService = sessionService;
            _templateRuleService = templateRuleService;
            _renameExecutionService = renameExecutionService;
            _filePickerService = filePickerService;
            _dialogService = dialogService;
        }

        public ObservableCollection<AudioItemViewModel> ToRenameItems { get; } = [];
        public ObservableCollection<AudioItemViewModel> DoNotRenameItems { get; } = [];
        public ObservableCollection<FolderItemViewModel> FolderItems { get; } = [];

        [ObservableProperty]
        private AudioItemViewModel? selectedToRenameItem;

        [ObservableProperty]
        private AudioItemViewModel? selectedDoNotRenameItem;

        [ObservableProperty]
        private FolderItemViewModel? selectedFolderItem;

        [ObservableProperty]
        private BitmapImage? selectedCover;

        [ObservableProperty]
        private string mainStatusText = Strings.READY;

        [ObservableProperty]
        private string loadedStatusText = $"{Strings.LOADED}: 0/0";

        [ObservableProperty]
        private bool isBusy;

        public bool IncludeSubFolders
        {
            get => Settings.Default.IncludeSubFolders;
            set
            {
                if (Settings.Default.IncludeSubFolders == value)
                {
                    return;
                }

                Settings.Default.IncludeSubFolders = value;
                Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public bool UsePlaceholderForMissingTags => Settings.Default.UsePlaceholderForMissingTags;
        public bool UseStrictModeForMissingTags => !Settings.Default.UsePlaceholderForMissingTags;

        public bool IsEnglishLanguage => Settings.Default.Language == "en";
        public bool IsSpanishLanguage => Settings.Default.Language == "es";

        public bool IsDarkTheme => string.Equals(Settings.Default.ThemeName, "Dark", StringComparison.OrdinalIgnoreCase);
        public bool IsLightTheme => string.Equals(Settings.Default.ThemeName, "Light", StringComparison.OrdinalIgnoreCase);

        public bool CanRename => !IsBusy && ToRenameItems.Count > 0;
        public bool HasSessionData => ToRenameItems.Count > 0 || DoNotRenameItems.Count > 0 || FolderItems.Count > 0;

        public async Task InitializeAsync()
        {
            await _sessionService.EnsureDatabaseAsync();
            if (await _sessionService.HasSavedSessionAsync())
            {
                bool loadPreviousSession = _dialogService.Confirm(
                    "A previous session was found. Load it now?",
                    "Previous Session");

                if (!loadPreviousSession)
                {
                    _dialogService.ShowWarning(
                        "The saved session will be deleted.",
                        "Previous Session");
                    await _sessionService.ClearSessionAsync();
                }
                else
                {
                    await Task.Run(() => _sessionService.RecalculateAllAsync(CreateRuleOptions()));
                }
            }

            await ReloadSnapshotAsync(showMissingFilesWarning: true);
        }

        [RelayCommand]
        private async Task AddFilesAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IReadOnlyList<string> files = _filePickerService.PickFiles();
            if (files.Count == 0)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                SessionIngestionResult result = await Task.Run(
                    () => _sessionService.AddFilesAsync(files, CreateRuleOptions()));
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
                ShowIngestionSummary(result);
            });
        }

        [RelayCommand]
        private async Task AddFoldersAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IReadOnlyList<string> folders = _filePickerService.PickFolders();
            if (folders.Count == 0)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                SessionIngestionResult result = await Task.Run(() => _sessionService.AddFoldersAsync(
                    folders,
                    IncludeSubFolders,
                    CreateRuleOptions()));

                await ReloadSnapshotAsync(showMissingFilesWarning: false);
                ShowIngestionSummary(result);
            });
        }

        [RelayCommand]
        private async Task LoadPreviousDataAsync()
        {
            if (IsBusy)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                await ReloadSnapshotAsync(showMissingFilesWarning: true);
            });
        }

        [RelayCommand(CanExecute = nameof(CanExecuteRenameAll))]
        private async Task RenameFilesAsync()
        {
            if (IsBusy || ToRenameItems.Count == 0)
            {
                return;
            }

            if (!_dialogService.Confirm(
                "All eligible files will be renamed now. Continue?",
                Strings.RENAME_FILES))
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                RenameBatchResult result = await _renameExecutionService.RenameAllAsync(_dialogService);
                await ReloadSnapshotAsync(showMissingFilesWarning: false);

                string summary = $"Done: {result.CompletedCount}\n" +
                                 $"Skipped: {result.SkippedCount}\n" +
                                 $"Failed: {result.FailedCount}\n" +
                                 $"Missing: {result.MissingCount}";

                _dialogService.ShowInfo(summary, Strings.RENAME_FILES);
            });
        }

        private bool CanExecuteRenameAll()
        {
            return CanRename;
        }

        [RelayCommand]
        private async Task OpenTemplateDialogAsync()
        {
            if (IsBusy)
            {
                return;
            }

            TemplateDialogResult? result = _dialogService.ShowTemplateDialog();
            if (result is null)
            {
                return;
            }

            bool settingsChanged =
                !string.Equals(result.Template, Settings.Default.DefaultTemplate, StringComparison.Ordinal)
                || result.MinTagsRequiredIndex != Settings.Default.MinTagsRequiredIndex
                || result.TrackNumRequired != Settings.Default.TrackNumRequired
                || result.TitleRequired != Settings.Default.TitleRequired
                || result.AlbumRequired != Settings.Default.AlbumRequired
                || result.AlbumArtistRequired != Settings.Default.AlbumArtistRequired
                || result.ArtistRequired != Settings.Default.ArtistRequired
                || result.YearRequired != Settings.Default.YearRequired;

            if (HasSessionData && settingsChanged)
            {
                bool recalculate = _dialogService.Confirm(
                    "Saving this rule will recalculate the current list. Continue?",
                    Strings.REPLACE_WITH);

                if (!recalculate)
                {
                    return;
                }
            }

            if (!settingsChanged)
            {
                return;
            }

            Settings.Default.DefaultTemplate = result.Template;
            Settings.Default.MinTagsRequiredIndex = result.MinTagsRequiredIndex;
            Settings.Default.TrackNumRequired = result.TrackNumRequired;
            Settings.Default.TitleRequired = result.TitleRequired;
            Settings.Default.AlbumRequired = result.AlbumRequired;
            Settings.Default.AlbumArtistRequired = result.AlbumArtistRequired;
            Settings.Default.ArtistRequired = result.ArtistRequired;
            Settings.Default.YearRequired = result.YearRequired;
            Settings.Default.Save();

            IReadOnlyList<string> repeatedTokens = _templateRuleService.GetRepeatedTokens(result.Template);
            if (repeatedTokens.Count > 0)
            {
                string warning = $"Repeated tags in template: {string.Join(", ", repeatedTokens)}";
                _dialogService.ShowWarning(warning, Strings.REPLACE_WITH);
            }

            await RunBusyAsync(async () =>
            {
                await Task.Run(() => _sessionService.RecalculateAllAsync(CreateRuleOptions()));
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
            });
        }

        [RelayCommand]
        private async Task SetMissingTagStrategyAsync(string? usePlaceholderValue)
        {
            if (IsBusy)
            {
                return;
            }

            if (!bool.TryParse(usePlaceholderValue, out bool usePlaceholder))
            {
                return;
            }

            if (Settings.Default.UsePlaceholderForMissingTags == usePlaceholder)
            {
                return;
            }

            if (HasSessionData)
            {
                bool recalculate = _dialogService.Confirm(
                    "Changing this setting will recalculate the current list. Continue?",
                    "Missing Tags");

                if (!recalculate)
                {
                    return;
                }
            }

            Settings.Default.UsePlaceholderForMissingTags = usePlaceholder;
            Settings.Default.Save();
            OnPropertyChanged(nameof(UsePlaceholderForMissingTags));
            OnPropertyChanged(nameof(UseStrictModeForMissingTags));

            await RunBusyAsync(async () =>
            {
                await Task.Run(() => _sessionService.RecalculateAllAsync(CreateRuleOptions()));
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
            });
        }

        [RelayCommand]
        private void SelectTheme(string? themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                return;
            }

            ThemeService.ChangeTheme(themeName);
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsLightTheme));
        }

        [RelayCommand]
        private void SelectLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return;
            }

            AppLanguageService.ChangeLanguage(languageCode);
            _dialogService.ShowWarning(Strings.TOGGLE_LANG_MSG, Strings.RESTARTING);
            App.RestartApp();
        }

        [RelayCommand]
        private void RestoreSettings()
        {
            Settings.Default.Language = "en";
            Settings.Default.DefaultTemplate = "<TrackNum>. <Title> - <Album> (<Year>)";
            Settings.Default.TrackNumRequired = false;
            Settings.Default.TitleRequired = true;
            Settings.Default.AlbumRequired = true;
            Settings.Default.AlbumArtistRequired = false;
            Settings.Default.ArtistRequired = false;
            Settings.Default.YearRequired = false;
            Settings.Default.IncludeSubFolders = true;
            Settings.Default.RepeatedFileKeepChoice = false;
            Settings.Default.UsePlaceholderForMissingTags = false;
            Settings.Default.Save();

            _dialogService.ShowInfo(Strings.SETTINGS_RESTORED, Strings.RESTORE_SETTINGS);
            App.RestartApp();
        }

        [RelayCommand]
        private void ExitApp()
        {
            if (HasSessionData)
            {
                bool confirmExit = _dialogService.Confirm(Strings.EXIT_MSG, $"{Strings.EXIT}?");
                if (!confirmExit)
                {
                    return;
                }
            }

            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void OpenAudioInExplorer(AudioItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            try
            {
                Process.Start("explorer.exe", $"/select,\"{item.FullPath}\"");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, Strings.EXCEPTION_MSG);
            }
        }

        [RelayCommand]
        private void PlayAudio(AudioItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            if (!File.Exists(item.FullPath))
            {
                _dialogService.ShowWarning("File not found.", Strings.PLAY_FILE);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, Strings.EXCEPTION_MSG);
            }
        }

        [RelayCommand]
        private async Task EditAudioTagsAsync(AudioItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            if (!File.Exists(item.FullPath))
            {
                _dialogService.ShowWarning(Strings.FILE_NOT_FOUND_MSG, Strings.EDIT_TAGS);
                return;
            }

            bool edited = _dialogService.EditMetadata(item.FullPath);
            if (!edited)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                await _sessionService.RefreshAudioFromDiskAsync(item.Id, CreateRuleOptions());
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
                _dialogService.ShowInfo(Strings.METADATA_EDIT_SUCCESS, Strings.EDIT_TAGS);
            });
        }

        [RelayCommand]
        private async Task RenameThisNowAsync(AudioItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            if (!_dialogService.Confirm(
                "This file will be renamed now. Continue?",
                Strings.RENAME_THIS_NOW))
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                RenameBatchResult result = await _renameExecutionService.RenameByIdsAsync([item.Id], _dialogService);
                await ReloadSnapshotAsync(showMissingFilesWarning: false);

                if (result.CompletedCount == 0 && result.SkippedCount == 0 && result.FailedCount == 0 && result.MissingCount == 0)
                {
                    _dialogService.ShowWarning("No changes were applied.", Strings.RENAME_THIS_NOW);
                }
            });
        }

        [RelayCommand]
        private async Task MoveToDoNotRenameAsync(AudioItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                await _sessionService.MarkAsDoNotRenameAsync(item.Id, "Manually excluded.");
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
            });
        }

        [RelayCommand]
        private async Task TryMoveToRenameAsync(AudioItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                bool canRename = await _sessionService.TryMoveToRenameAsync(item.Id, CreateRuleOptions());
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
                if (!canRename)
                {
                    _dialogService.ShowWarning(
                        "The file still does not satisfy the current rename rule.",
                        Strings.DO_NOT_RENAME);
                }
            });
        }

        [RelayCommand]
        private void OpenFolderInExplorer(FolderItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            try
            {
                Process.Start("explorer.exe", item.Path);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, Strings.EXCEPTION_MSG);
            }
        }

        [RelayCommand]
        private async Task RemoveFolderFromSessionAsync(FolderItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            if (!_dialogService.Confirm(
                "All files from this folder and its subfolders will be removed from the current session. Continue?",
                Strings.REMOVE_FROM_LIST))
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                await _sessionService.RemoveFolderAsync(item.Id);
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
            });
        }

        // TODO: Implement a safe DeleteFile workflow with explicit confirmation and rollback strategy.
        // TODO: Implement a safe DeleteFolder workflow with explicit confirmation and rollback strategy.

        partial void OnSelectedToRenameItemChanged(AudioItemViewModel? value)
        {
            if (value is not null && SelectedDoNotRenameItem is not null)
            {
                SelectedDoNotRenameItem = null;
            }

            UpdateSelectedAudio(value);
        }

        partial void OnSelectedDoNotRenameItemChanged(AudioItemViewModel? value)
        {
            if (value is not null && SelectedToRenameItem is not null)
            {
                SelectedToRenameItem = null;
            }

            UpdateSelectedAudio(value);
        }

        partial void OnIsBusyChanged(bool value)
        {
            RenameFilesCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanRename));
        }

        private void UpdateSelectedAudio(AudioItemViewModel? item)
        {
            SelectedCover = null;
            if (item is null)
            {
                MainStatusText = Strings.READY;
                return;
            }

            MainStatusText = item.FullPath;
            try
            {
                using TagLib.File file = TagLib.File.Create(item.FullPath);
                if (file.Tag.Pictures.Length > 0)
                {
                    SelectedCover = Multimedia.GetBitmapImage(file.Tag.Pictures[0].Data.Data);
                }
            }
            catch (Exception)
            {
                SelectedCover = null;
            }
        }

        private RenameRuleOptions CreateRuleOptions()
        {
            MissingTagStrategy strategy = Settings.Default.UsePlaceholderForMissingTags
                ? MissingTagStrategy.UsePlaceholder
                : MissingTagStrategy.Strict;

            return new RenameRuleOptions
            {
                Template = Settings.Default.DefaultTemplate,
                MissingTagStrategy = strategy,
                PlaceholderText = Strings.UNKNOWN
            };
        }

        private async Task ReloadSnapshotAsync(bool showMissingFilesWarning)
        {
            SelectedToRenameItem = null;
            SelectedDoNotRenameItem = null;
            SelectedCover = null;
            MainStatusText = Strings.READY;

            SessionSnapshot snapshot = await _sessionService.LoadSnapshotAsync();

            ReplaceCollection(ToRenameItems, snapshot.ToRename.Select(i => new AudioItemViewModel(i)));
            ReplaceCollection(DoNotRenameItems, snapshot.DoNotRename.Select(i => new AudioItemViewModel(i)));
            ReplaceCollection(FolderItems, snapshot.Folders.Select(i => new FolderItemViewModel(i)));

            int total = ToRenameItems.Count + DoNotRenameItems.Count;
            LoadedStatusText = $"{Strings.LOADED}: {total}/{total}";

            OnPropertyChanged(nameof(CanRename));
            OnPropertyChanged(nameof(HasSessionData));
            RenameFilesCommand.NotifyCanExecuteChanged();

            if (showMissingFilesWarning && snapshot.MissingFilesCount > 0)
            {
                _dialogService.ShowWarning(
                    "Some files were not found and were moved to the 'Do Not Rename' section.",
                    "Missing Files");
            }
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (T item in source)
            {
                target.Add(item);
            }
        }

        private async Task RunBusyAsync(Func<Task> action)
        {
            IsBusy = true;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message, Strings.EXCEPTION_MSG);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowIngestionSummary(SessionIngestionResult result)
        {
            if (result.SkippedDuplicates == 0 && result.UnreadableCount == 0)
            {
                return;
            }

            string summary = $"Added: {result.AddedCount}\n" +
                             $"Duplicates ignored: {result.SkippedDuplicates}\n" +
                             $"Unreadable metadata: {result.UnreadableCount}";

            _dialogService.ShowInfo(summary, Strings.LOADING);
        }
    }
}
