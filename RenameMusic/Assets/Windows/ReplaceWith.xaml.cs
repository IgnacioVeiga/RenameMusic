using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RenameMusic.Assets
{
    /// <summary>
    /// Interaction logic for ReplaceWith.xaml
    /// </summary>
    public partial class ReplaceWith : Window
    {
        private readonly List<string> _requiredTags = new();

        private readonly Dictionary<string, string> MetadataMap = new()
        {
            { "<TrackNum>", "Track Nº" },
            { "<Title>", "Title" },
            { "<Artist>", "Artist" },
            { "<Album>", "Album" },
            { "<AlbumArtist>", "Album artist" },
            { "<Year>", "Year" },
        };

        private bool CheckAllTagsRequired()
        {
            return _requiredTags.TrueForAll(tag => ToReplace.Text.Contains(tag));
        }

        public ReplaceWith()
        {
            InitializeComponent();

            // Tags items
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
            MinTagsReqCBOX.SelectedIndex = Settings.Default.MinTagsRequiredIndex;

            trackNumRequired.IsChecked = Settings.Default.TrackNumRequired;
            titleRequired.IsChecked = Settings.Default.TitleRequired;
            artistRequired.IsChecked = Settings.Default.ArtistRequired;
            albumRequired.IsChecked = Settings.Default.AlbumRequired;
            albumArtistRequired.IsChecked = Settings.Default.AlbumArtistRequired;
            yearRequired.IsChecked = Settings.Default.YearRequired;
        }

        private void ApplyBTN_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultTemplate = ToReplace.Text;
            Settings.Default.MinTagsRequiredIndex = MinTagsReqCBOX.SelectedIndex;
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
            ComboBox comboBox = (ComboBox)sender;

            // We omit the placeholder (index 0)
            if (comboBox.SelectedIndex != 0)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                rtbox.Focus();
                rtbox.CaretPosition.InsertTextInRun(selectedItem.Tag.ToString());
                comboBox.SelectedIndex = 0;
            }
        }

        // ToDo: Show a "Warning" message only when necessary and refactor
        private void RTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ToReplace == null) return;

            // It must not be an empty space or similar
            if (string.IsNullOrWhiteSpace(ToReplace.Text))
            {
                ApplyBTN.IsEnabled = false;
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: {Strings.CANNOT_BE_EMPTY}";
                return;
            }

            // It must contain at least 1 "tag" of those marked as required
            ApplyBTN.IsEnabled = CheckAllTagsRequired();
            if (!ApplyBTN.IsEnabled)
            {
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: {Strings.REQ_TAG_MISS_MSG}";
                return;
            }

            // I replace only the invalid characters that we use to recognize the tags,
            // so any remaining invalid characters are not part of our code.
            string temp = ToReplace.Text;
            foreach (string tag in MetadataMap.Keys)
            {
                temp = temp.Replace(tag, tag.Replace("<", "_").Replace(">", "_"));
            }
            if (!FilenameFunctions.IsValidFileName(temp))
            {
                ApplyBTN.IsEnabled = false;
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: {Strings.INVALID_TEMPLATE_MSG}";
            }
            else
            {
                ApplyBTN.IsEnabled = true;
                WarningMSG.Text = string.Empty;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbox = sender as ComboBox;
            if (cbox.SelectedIndex == 0)
            {
                stackPanelTagsReq.Visibility = Visibility.Hidden;
                stackPanelNullCharacter.Visibility = Visibility.Visible;
            }
            else if (cbox.SelectedIndex == 1)
            {
                stackPanelTagsReq.Visibility = Visibility.Visible;
                stackPanelNullCharacter.Visibility = Visibility.Visible;
            }
            else
            {
                stackPanelTagsReq.Visibility = Visibility.Collapsed;
                stackPanelNullCharacter.Visibility = Visibility.Collapsed;
            }
        }

        private void TagRequired_CheckChanged(object sender, RoutedEventArgs e)
        {
            _requiredTags.Clear();
            if (trackNumRequired.IsChecked == true) _requiredTags.Add("<TrackNum>");
            if (titleRequired.IsChecked == true) _requiredTags.Add("<Title>");
            if (albumRequired.IsChecked == true) _requiredTags.Add("<Album>");
            if (albumArtistRequired.IsChecked == true) _requiredTags.Add("<AlbumArtist>");
            if (artistRequired.IsChecked == true) _requiredTags.Add("<Artist>");
            if (yearRequired.IsChecked == true) _requiredTags.Add("<Year>");

            if (!CheckAllTagsRequired())
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: At least one tag marked as required is missing.";
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: {Strings.REQ_TAG_MISS_MSG}";
                WarningMSG.Text = string.Empty; // ToDo: Check again "ToReplace.Text"
        }
    }
}
