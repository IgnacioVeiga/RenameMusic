using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System.Windows;
using System.Windows.Controls;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for Template.xaml
    /// </summary>
    public partial class RenamingRule : Window
    {
        readonly string[] tags = { "<tn>", "<t>", "<a>", "<aAt>", "<At>", "<yr>" };

        public RenamingRule()
        {
            InitializeComponent();
            renamingRule.Text = Settings.Default.DefaultTemplate;
            titleRequired.Content = $"<t> = {Strings.TITLE}";
            albumRequired.Content = $"<a> = {Strings.ALBUM}";
            albumArtistRequired.Content = $"<aAt> = {Strings.ALBUM_ARTIST}";
            artistRequired.Content = $"<At> = {Strings.ARTIST}";
            simbols.Text = $"<tn> = {Strings.TRACK_NUM}\r\n<yr> = {Strings.YEAR}";
        }

        private void ApplyBTN_Click(object sender, RoutedEventArgs e)
        {
            string newTemplate = renamingRule.Text;

            foreach (var tag in tags)
            {
                newTemplate = newTemplate.Replace(tag, tag.Replace("<", "_").Replace(">", "_"));
            }

            if (FilenameFunctions.IsValidFileName(newTemplate))
            {
                Settings.Default.DefaultTemplate = renamingRule.Text;
                Settings.Default.Save();
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(Strings.INVALID_TEMPLATE_MSG, Strings.NOT_ALLOWED, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Template_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(renamingRule.Text))
            {
                ApplyBTN.IsEnabled = false;
            }
            else
            {
                foreach (var tag in tags)
                {
                    if (renamingRule.Text.Contains(tag))
                    {
                        ApplyBTN.IsEnabled = true;
                        break;
                    }
                    else
                    {
                        ApplyBTN.IsEnabled = false;
                    }
                }
            }
        }
    }
}
