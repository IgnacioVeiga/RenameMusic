using RenameMusic.Properties;
using RenameMusic.Resources.Languages;
using RenameMusic.Services;
using RenameMusic.ViewModels;
using System.Diagnostics;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex;

        App()
        {
            //SetDropDownMenuToBeRightAligned();
            AppLanguageService.ChangeLanguage(Settings.Default.Language);
        }

        internal static void RestartApp()
        {
            try
            {
                Process.Start(Environment.ProcessPath);
                Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            const bool initiallyOwned = true;
            const string name = "RenameMusic";
            _mutex = new Mutex(initiallyOwned, name, out bool createdNew);
            if (createdNew)
            {
                Exit += CloseMutexHandler;
            }
            else
            {
                MessageBox.Show(Strings.MULTI_INSTANCE_MSG);
                Current.Shutdown();
            }
            base.OnStartup(e);

            if (!createdNew)
            {
                return;
            }

            try
            {
                TemplateRuleService templateRuleService = new();
                SessionService sessionService = new(templateRuleService);
                RenameExecutionService renameExecutionService = new(sessionService);
                IFilePickerService filePickerService = new FilePickerService();
                IDialogService dialogService = new DialogService();

                MainWindowViewModel mainWindowViewModel = new(
                    sessionService,
                    templateRuleService,
                    renameExecutionService,
                    filePickerService,
                    dialogService);

                Views.MainWindow window = new()
                {
                    DataContext = mainWindowViewModel
                };

                MainWindow = window;
                ThemeService.LoadTheme();
                window.Show();
                await mainWindowViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected virtual void CloseMutexHandler(object sender, EventArgs e)
        {
            _mutex?.Close();
        }

        // TODO: Move to another file
        internal static bool CanBeRenamed(TagLib.Tag tags)
        {
            int tagsRequiredCount = 0, tagsNotEmptyCount = 0;

            tagsRequiredCount = Settings.Default.TrackNumRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Settings.Default.TitleRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Settings.Default.AlbumRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Settings.Default.AlbumArtistRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Settings.Default.ArtistRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Settings.Default.YearRequired ? tagsRequiredCount + 1 : tagsRequiredCount;

            if (Settings.Default.TrackNumRequired && tags.Track > 0)
                tagsNotEmptyCount++;
            if (Settings.Default.TitleRequired && !string.IsNullOrWhiteSpace(tags.Title))
                tagsNotEmptyCount++;
            if (Settings.Default.AlbumRequired && !string.IsNullOrWhiteSpace(tags.Album))
                tagsNotEmptyCount++;
            if (Settings.Default.AlbumArtistRequired && !string.IsNullOrWhiteSpace(tags.JoinedAlbumArtists))
                tagsNotEmptyCount++;
            if (Settings.Default.ArtistRequired && !string.IsNullOrWhiteSpace(tags.JoinedPerformers))
                tagsNotEmptyCount++;
            if (Settings.Default.YearRequired && tags.Year > 0)
                tagsNotEmptyCount++;

            return tagsRequiredCount == tagsNotEmptyCount;
        }

        //// Source: https://stackoverflow.com/a/67114984
        //private static void SetDropDownMenuToBeRightAligned()
        //{
        //    FieldInfo menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);

        //    static void setAlignmentValue(FieldInfo menuDropAlignmentField)
        //    {
        //        if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
        //    }

        //    setAlignmentValue(menuDropAlignmentField);

        //    SystemParameters.StaticPropertyChanged += (sender, e) => setAlignmentValue(menuDropAlignmentField);
        //}
    }
}
