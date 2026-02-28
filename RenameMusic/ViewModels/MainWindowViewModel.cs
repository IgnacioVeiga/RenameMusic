using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RenameMusic.Models;
using RenameMusic.Properties;
using RenameMusic.Resources.Languages;
using RenameMusic.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RenameMusic.ViewModels
{
    /// <summary>
    /// Coordinates the main screen workflow: session lifecycle, ingestion, rule recalculation, and rename execution.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ISessionService _sessionService;
        private readonly ITemplateRuleService _templateRuleService;
        private readonly IRenameExecutionService _renameExecutionService;
        private readonly IFilePickerService _filePickerService;
        private readonly IFileDeletionService _fileDeletionService;
        private readonly IDialogService _dialogService;

        public MainWindowViewModel(
            ISessionService sessionService,
            ITemplateRuleService templateRuleService,
            IRenameExecutionService renameExecutionService,
            IFilePickerService filePickerService,
            IFileDeletionService fileDeletionService,
            IDialogService dialogService)
        {
            _sessionService = sessionService;
            _templateRuleService = templateRuleService;
            _renameExecutionService = renameExecutionService;
            _filePickerService = filePickerService;
            _fileDeletionService = fileDeletionService;
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
        public string MainWindowTitle => L("MAIN_WINDOW_TITLE", "Rename Music (Beta)");
        public string MissingTagsModeText => L("MISSING_TAGS_MODE", "Missing tags mode");
        public string MissingTagsStrictText => L("STRICT", "Strict");
        public string MissingTagsUseUnknownText => L("USE_UNKNOWN_OR_TRANSLATED", "Use Unknown/Desconocido");
        public string ConflictPolicyText => L("CONFLICT_POLICY", "Conflict policy");
        public string ConflictAskEveryTimeText => L("ASK_EVERY_TIME", "Ask every time");
        public string ConflictAlwaysReplaceText => L("ALWAYS_REPLACE", "Always replace");
        public string ConflictAlwaysSkipText => L("ALWAYS_SKIP", "Always skip");
        public string ConflictAlwaysRenameWithNumberText => L("ALWAYS_RENAME_WITH_NUMBER", "Always rename with number");
        public string ThemeDarkText => L("DARK", "Dark");
        public string ThemeLightText => L("LIGHT", "Light");
        public string LanguageEnglishText => L("ENGLISH", "English");
        public string LanguageSpanishText => L("SPANISH", "Español");
        public string ReasonColumnHeaderText => L("REASON", "Reason");

        public bool IsDarkTheme => string.Equals(Settings.Default.ThemeName, "Dark", StringComparison.OrdinalIgnoreCase);
        public bool IsLightTheme => string.Equals(Settings.Default.ThemeName, "Light", StringComparison.OrdinalIgnoreCase);
        public bool IsConflictPolicyAsk => string.Equals(GetConflictPolicyMode(), "Ask", StringComparison.OrdinalIgnoreCase);
        public bool IsConflictPolicyReplace => string.Equals(GetConflictPolicyMode(), "Replace", StringComparison.OrdinalIgnoreCase);
        public bool IsConflictPolicySkip => string.Equals(GetConflictPolicyMode(), "Skip", StringComparison.OrdinalIgnoreCase);
        public bool IsConflictPolicyRenameWithNumber => string.Equals(GetConflictPolicyMode(), "RenameWithNumber", StringComparison.OrdinalIgnoreCase);

        public bool CanRename => !IsBusy && ToRenameItems.Count > 0;
        public bool HasSessionData => ToRenameItems.Count > 0 || DoNotRenameItems.Count > 0 || FolderItems.Count > 0;

        /// <summary>
        /// Initializes persistence and optionally restores the previous session after explicit user confirmation.
        /// </summary>
        public async Task InitializeAsync()
        {
            await _sessionService.EnsureDatabaseAsync();
            if (await _sessionService.HasSavedSessionAsync())
            {
                string previousSessionTitle = L("PREVIOUS_SESSION_TITLE", "Previous Session");
                bool loadPreviousSession = _dialogService.Confirm(
                    L("PREVIOUS_SESSION_FOUND_MSG", "A previous session was found. Load it now?"),
                    previousSessionTitle);

                if (!loadPreviousSession)
                {
                    _dialogService.ShowWarning(
                        L("PREVIOUS_SESSION_DELETE_MSG", "The saved session will be deleted."),
                        previousSessionTitle);
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
                L("RENAME_ALL_CONFIRM_MSG", "All eligible files will be renamed now. Continue?"),
                Strings.RENAME_FILES))
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                ConflictResolutionAction? defaultConflictAction = GetDefaultConflictAction();
                RenameBatchResult result = defaultConflictAction.HasValue
                    ? await Task.Run(() => _renameExecutionService.RenameAllAsync(
                        _dialogService,
                        defaultConflictAction.Value))
                    : await _renameExecutionService.RenameAllAsync(_dialogService);
                await ReloadSnapshotAsync(showMissingFilesWarning: false);

                string summary = $"{L("SUMMARY_DONE", "Done")}: {result.CompletedCount}\n" +
                                 $"{L("SUMMARY_SKIPPED", "Skipped")}: {result.SkippedCount}\n" +
                                 $"{L("SUMMARY_FAILED", "Failed")}: {result.FailedCount}\n" +
                                 $"{L("SUMMARY_MISSING", "Missing")}: {result.MissingCount}";

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
                    L("TEMPLATE_RECALCULATE_CONFIRM_MSG", "Saving this rule will recalculate the current list. Continue?"),
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
                string repeatedPrefix = L("TEMPLATE_REPEATED_TAGS_MSG", "Repeated tags in template");
                string warning = $"{repeatedPrefix}: {string.Join(", ", repeatedTokens)}";
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
                    L("MISSING_TAGS_RECALCULATE_CONFIRM_MSG", "Changing this setting will recalculate the current list. Continue?"),
                    L("MISSING_TAGS_TITLE", "Missing Tags"));

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
        private void SetConflictPolicy(string? policyMode)
        {
            string normalizedMode = policyMode switch
            {
                "Ask" => "Ask",
                "Replace" => "Replace",
                "Skip" => "Skip",
                "RenameWithNumber" => "RenameWithNumber",
                _ => "Ask"
            };

            if (string.Equals(GetConflictPolicyMode(), normalizedMode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Settings.Default.ConflictPolicyMode = normalizedMode;
            Settings.Default.Save();

            OnPropertyChanged(nameof(IsConflictPolicyAsk));
            OnPropertyChanged(nameof(IsConflictPolicyReplace));
            OnPropertyChanged(nameof(IsConflictPolicySkip));
            OnPropertyChanged(nameof(IsConflictPolicyRenameWithNumber));
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
            Settings.Default.ConflictPolicyMode = "Ask";
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
                _dialogService.ShowWarning(Strings.FILE_NOT_FOUND_MSG, Strings.PLAY_FILE);
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
                L("RENAME_SINGLE_CONFIRM_MSG", "This file will be renamed now. Continue?"),
                Strings.RENAME_THIS_NOW))
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                ConflictResolutionAction? defaultConflictAction = GetDefaultConflictAction();
                RenameBatchResult result = defaultConflictAction.HasValue
                    ? await Task.Run(() => _renameExecutionService.RenameByIdsAsync(
                        [item.Id],
                        _dialogService,
                        defaultConflictAction.Value))
                    : await _renameExecutionService.RenameByIdsAsync(
                        [item.Id],
                        _dialogService);
                await ReloadSnapshotAsync(showMissingFilesWarning: false);

                if (result.CompletedCount == 0 && result.SkippedCount == 0 && result.FailedCount == 0 && result.MissingCount == 0)
                {
                    _dialogService.ShowWarning(L("NO_CHANGES_APPLIED_MSG", "No changes were applied."), Strings.RENAME_THIS_NOW);
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
                await _sessionService.MarkAsDoNotRenameAsync(
                    item.Id,
                    NotRenamableReasonCodec.Create(NotRenamableReasonCodes.ManuallyExcluded));
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
                        L("RULE_NOT_SATISFIED_MSG", "The file still does not satisfy the current rename rule."),
                        Strings.DO_NOT_RENAME);
                }
            });
        }

        [RelayCommand]
        private async Task DeleteAudioFileAsync(AudioItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            bool confirmDelete = _dialogService.Confirm(
                L("DELETE_FILE_CONFIRM_MSG", "This file will be moved to the Recycle Bin and removed from the current session. Continue?"),
                Strings.DELETE_FILE);
            if (!confirmDelete)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                if (File.Exists(item.FullPath))
                {
                    _fileDeletionService.DeleteFile(item.FullPath);
                }
                else
                {
                    _dialogService.ShowWarning(
                        L("FILE_NOT_FOUND_REMOVE_ONLY_MSG", "File not found on disk. It will only be removed from the session."),
                        Strings.DELETE_FILE);
                }

                await _sessionService.RemoveAudioAsync(item.Id);
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
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
                L("REMOVE_FOLDER_CONFIRM_MSG", "All files from this folder and its subfolders will be removed from the current session. Continue?"),
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

        [RelayCommand]
        private async Task DeleteFolderAsync(FolderItemViewModel? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            bool confirmDelete = _dialogService.Confirm(
                L("DELETE_FOLDER_CONFIRM_MSG", "This folder and its content will be moved to the Recycle Bin and removed from the current session. Continue?"),
                Strings.DELETE_FOLDER);
            if (!confirmDelete)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                if (Directory.Exists(item.Path))
                {
                    _fileDeletionService.DeleteDirectory(item.Path);
                }
                else
                {
                    _dialogService.ShowWarning(
                        L("FOLDER_NOT_FOUND_REMOVE_ONLY_MSG", "Folder not found on disk. It will only be removed from the session."),
                        Strings.DELETE_FOLDER);
                }

                await _sessionService.RemoveFolderAsync(item.Id);
                await ReloadSnapshotAsync(showMissingFilesWarning: false);
            });
        }

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

        /// <summary>
        /// Builds a snapshot of the current rule settings to evaluate files consistently across services.
        /// </summary>
        private RenameRuleOptions CreateRuleOptions()
        {
            MissingTagStrategy strategy = Settings.Default.UsePlaceholderForMissingTags
                ? MissingTagStrategy.UsePlaceholder
                : MissingTagStrategy.Strict;

            return new RenameRuleOptions
            {
                Template = Settings.Default.DefaultTemplate,
                MissingTagStrategy = strategy,
                PlaceholderText = Strings.UNKNOWN,
                MinTagsRequiredIndex = Settings.Default.MinTagsRequiredIndex,
                RequiredTokens = GetRequiredTokens()
            };
        }

        private static IReadOnlySet<string> GetRequiredTokens()
        {
            HashSet<string> required = new(StringComparer.Ordinal);

            if (Settings.Default.TrackNumRequired)
            {
                required.Add("<TrackNum>");
            }

            if (Settings.Default.TitleRequired)
            {
                required.Add("<Title>");
            }

            if (Settings.Default.AlbumRequired)
            {
                required.Add("<Album>");
            }

            if (Settings.Default.AlbumArtistRequired)
            {
                required.Add("<AlbumArtist>");
            }

            if (Settings.Default.ArtistRequired)
            {
                required.Add("<Artist>");
            }

            if (Settings.Default.YearRequired)
            {
                required.Add("<Year>");
            }

            return required;
        }

        private static string GetConflictPolicyMode()
        {
            string? mode = Settings.Default.ConflictPolicyMode;
            return mode switch
            {
                "Ask" => "Ask",
                "Replace" => "Replace",
                "Skip" => "Skip",
                "RenameWithNumber" => "RenameWithNumber",
                _ => "Ask"
            };
        }

        private static ConflictResolutionAction? GetDefaultConflictAction()
        {
            return GetConflictPolicyMode() switch
            {
                "Replace" => ConflictResolutionAction.Replace,
                "Skip" => ConflictResolutionAction.Skip,
                "RenameWithNumber" => ConflictResolutionAction.RenameWithNumber,
                _ => null
            };
        }

        /// <summary>
        /// Reloads UI collections from persistence and optionally warns when stored paths no longer exist.
        /// </summary>
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
                    L("MISSING_FILES_WARNING_MSG", "Some files were not found and were moved to the 'Do Not Rename' section."),
                    L("MISSING_FILES_TITLE", "Missing Files"));
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

        /// <summary>
        /// Wraps async commands with busy-state and centralized exception reporting.
        /// </summary>
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

        /// <summary>
        /// Shows ingestion diagnostics only when duplicates or unreadable files were detected.
        /// </summary>
        private void ShowIngestionSummary(SessionIngestionResult result)
        {
            if (result.SkippedDuplicates == 0 && result.UnreadableCount == 0)
            {
                return;
            }

            string summary = $"{L("SUMMARY_ADDED", "Added")}: {result.AddedCount}\n" +
                             $"{L("SUMMARY_DUPLICATES_IGNORED", "Duplicates ignored")}: {result.SkippedDuplicates}\n" +
                             $"{L("SUMMARY_UNREADABLE_METADATA", "Unreadable metadata")}: {result.UnreadableCount}";

            _dialogService.ShowInfo(summary, Strings.LOADING);
        }

        private static string L(string key, string fallback)
        {
            return Strings.ResourceManager.GetString(key, Strings.Culture) ?? fallback;
        }
    }
}
