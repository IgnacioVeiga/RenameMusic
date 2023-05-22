using RenameMusic.Properties;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RenameMusic.Assets
{
    /// <summary>
    /// Interaction logic for ReplaceWith.xaml
    /// </summary>
    public partial class ReplaceWith : Window
    {
        private readonly Dictionary<string, string> MetadataMap = new()
        {
            { "<TrackNum>", "Track Nº" },
            { "<Title>", "Title" },
            { "<Artist>", "Artist" },
            { "<Album>", "Album" },
            { "<AlbumArtist>", "Album artist" },
            { "<Year>", "Year" },
        };

        public ReplaceWith()
        {
            InitializeComponent();

            foreach (var metadata in MetadataMap)
            {
                ComboBoxItem tagItem = new()
                {
                    Tag = metadata.Key,
                    Content = metadata.Value
                };
                tagsBox.Items.Add(tagItem);
            }

            ToReplace.Text = Settings.Default.DefaultTemplate;
        }

        private void ApplyBTN_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultTemplate = ToReplace.Text;
            Settings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TagsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = ((ComboBox)sender).SelectedItem as ComboBoxItem;
            rtbox.Focus();
            rtbox.CaretPosition.InsertTextInRun(item.Tag.ToString());
        }
    }
}
