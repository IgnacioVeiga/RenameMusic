using System.ComponentModel;
using System.Configuration;

namespace RenameMusic.Properties
{
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    public sealed partial class Settings
    {
        public Settings()
        {
            // To add event handlers for saving and changing settings, uncomment the lines below:

            SettingChanging += SettingChangingEventHandler;
            SettingsSaving += SettingsSavingEventHandler;
        }

        private void SettingChangingEventHandler(object sender, SettingChangingEventArgs e)
        {
            // Add code to handle the SettingChanging event here.
        }

        private void SettingsSavingEventHandler(object sender, CancelEventArgs e)
        {
            // Add code to handle the SettingsSaving event here.
        }

        internal static bool CanBeRenamed(TagLib.Tag tags)
        {
            int tagsRequiredCount = 0, tagsNotEmptyCount = 0;

            tagsRequiredCount = Default.TrackNumRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Default.TitleRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Default.AlbumRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Default.AlbumArtistRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Default.ArtistRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
            tagsRequiredCount = Default.YearRequired ? tagsRequiredCount + 1 : tagsRequiredCount;

            if (Default.TrackNumRequired && tags.Track > 0)
                tagsNotEmptyCount++;
            if (Default.TitleRequired && !string.IsNullOrWhiteSpace(tags.Title))
                tagsNotEmptyCount++;
            if (Default.AlbumRequired && !string.IsNullOrWhiteSpace(tags.Album))
                tagsNotEmptyCount++;
            if (Default.AlbumArtistRequired && !string.IsNullOrWhiteSpace(tags.JoinedAlbumArtists))
                tagsNotEmptyCount++;
            if (Default.ArtistRequired && !string.IsNullOrWhiteSpace(tags.JoinedPerformers))
                tagsNotEmptyCount++;
            if (Default.YearRequired && tags.Year > 0)
                tagsNotEmptyCount++;

            return tagsRequiredCount == tagsNotEmptyCount;
        }
    }
}
