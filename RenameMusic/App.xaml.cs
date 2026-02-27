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
        private static Mutex? _mutex;

        App()
        {
            //SetDropDownMenuToBeRightAligned();
            AppLanguageService.ChangeLanguage(Settings.Default.Language);
        }

        internal static void RestartApp()
        {
            try
            {
                string? processPath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(processPath))
                {
                    MessageBox.Show(Strings.EXCEPTION_MSG, Strings.RESTARTING, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Process.Start(processPath);
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
                ITemplateRuleService templateRuleService = new TemplateRuleService();
                ISessionService sessionService = new SessionService(templateRuleService);
                IRenameExecutionService renameExecutionService = new RenameExecutionService(sessionService);
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
