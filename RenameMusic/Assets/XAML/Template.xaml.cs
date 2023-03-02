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
    public partial class Template : Window
    {
        readonly string[] tags = { "<tn>", "<t>", "<a>", "<aAt>", "<At>", "<yr>" };

        public Template()
        {
            InitializeComponent();
            template.Text = Settings.Default.DefaultTemplate;
            simbols.Text = $"<tn> = {Strings.TRACK_NUM}\r\n<t> = {Strings.TITLE}\r\n<a> =" +
                $" {Strings.ALBUM}\r\n<aAt> = {Strings.ALBUM_ARTIST}\r\n<At> = {Strings.ARTIST}\r\n<yr> = {Strings.YEAR}";
        }

        private void ApplyBTN_Click(object sender, RoutedEventArgs e)
        {
            string newTemplate = template.Text;

            foreach (var tag in tags)
            {
                newTemplate = newTemplate.Replace(tag, tag.Replace("<", "_").Replace(">", "_"));
            }

            if (FilenameFunctions.IsValidFileName(newTemplate))
            {
                Settings.Default.DefaultTemplate = template.Text;
                Settings.Default.Save();
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
            if (string.IsNullOrWhiteSpace(template.Text))
            {
                apply.IsEnabled = false;
            }
            else
            {
                foreach (var tag in tags)
                {
                    if (template.Text.Contains(tag))
                    {
                        apply.IsEnabled = true;
                        break;
                    }
                    else apply.IsEnabled = false;
                }
            }
        }
    }
}
