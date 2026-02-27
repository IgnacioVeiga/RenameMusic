using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RenameMusic.Models;
using RenameMusic.Properties;
using RenameMusic.Resources.Languages;

namespace RenameMusic.ViewModels
{
    public partial class ReplaceWithViewModel : ObservableObject
    {
        private static readonly string[] SupportedTags =
        [
            "<TrackNum>",
            "<Title>",
            "<Album>",
            "<AlbumArtist>",
            "<Artist>",
            "<Year>"
        ];

        [ObservableProperty]
        private string template;

        [ObservableProperty]
        private int minTagsRequiredIndex;

        [ObservableProperty]
        private bool trackNumRequired;

        [ObservableProperty]
        private bool titleRequired;

        [ObservableProperty]
        private bool albumRequired;

        [ObservableProperty]
        private bool albumArtistRequired;

        [ObservableProperty]
        private bool artistRequired;

        [ObservableProperty]
        private bool yearRequired;

        [ObservableProperty]
        private string warningMessage = string.Empty;

        [ObservableProperty]
        private bool canApply;

        public ReplaceWithViewModel()
        {
            template = Settings.Default.DefaultTemplate;
            minTagsRequiredIndex = Settings.Default.MinTagsRequiredIndex;
            trackNumRequired = Settings.Default.TrackNumRequired;
            titleRequired = Settings.Default.TitleRequired;
            albumRequired = Settings.Default.AlbumRequired;
            albumArtistRequired = Settings.Default.AlbumArtistRequired;
            artistRequired = Settings.Default.ArtistRequired;
            yearRequired = Settings.Default.YearRequired;

            ValidateTemplate();
            ApplyCommand.NotifyCanExecuteChanged();
        }

        public TemplateDialogResult? Result { get; private set; }
        public event Action<bool?>? CloseRequested;

        [RelayCommand]
        private void InsertTag(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            Template += token;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteApply))]
        private void Apply()
        {
            ValidateTemplate();
            if (!CanApply)
            {
                return;
            }

            Result = new TemplateDialogResult
            {
                Template = Template,
                MinTagsRequiredIndex = MinTagsRequiredIndex,
                TrackNumRequired = TrackNumRequired,
                TitleRequired = TitleRequired,
                AlbumRequired = AlbumRequired,
                AlbumArtistRequired = AlbumArtistRequired,
                ArtistRequired = ArtistRequired,
                YearRequired = YearRequired
            };

            CloseRequested?.Invoke(true);
        }

        private bool CanExecuteApply()
        {
            return CanApply;
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(false);
        }

        partial void OnTemplateChanged(string value)
        {
            Revalidate();
        }

        partial void OnMinTagsRequiredIndexChanged(int value)
        {
            Revalidate();
        }

        partial void OnTrackNumRequiredChanged(bool value)
        {
            Revalidate();
        }

        partial void OnTitleRequiredChanged(bool value)
        {
            Revalidate();
        }

        partial void OnAlbumRequiredChanged(bool value)
        {
            Revalidate();
        }

        partial void OnAlbumArtistRequiredChanged(bool value)
        {
            Revalidate();
        }

        partial void OnArtistRequiredChanged(bool value)
        {
            Revalidate();
        }

        partial void OnYearRequiredChanged(bool value)
        {
            Revalidate();
        }

        private void Revalidate()
        {
            ValidateTemplate();
            ApplyCommand.NotifyCanExecuteChanged();
        }

        private void ValidateTemplate()
        {
            if (string.IsNullOrWhiteSpace(Template))
            {
                CanApply = false;
                WarningMessage = $"{Strings.NOT_ALLOWED}: {Strings.CANNOT_BE_EMPTY}";
                return;
            }

            if (!HasAtLeastOneTag(Template))
            {
                CanApply = false;
                WarningMessage = $"{Strings.NOT_ALLOWED}: Template must contain at least one tag.";
                return;
            }

            if (!HasAllRequiredTags(Template))
            {
                CanApply = false;
                WarningMessage = $"{Strings.NOT_ALLOWED}: {Strings.REQ_TAG_MISS_MSG}";
                return;
            }

            string temp = Template;
            foreach (string tag in SupportedTags)
            {
                temp = temp.Replace(tag, tag.Replace("<", "_").Replace(">", "_"), StringComparison.Ordinal);
            }

            if (!FilenameFunctions.IsValidFileName(temp))
            {
                CanApply = false;
                WarningMessage = $"{Strings.NOT_ALLOWED}: {Strings.INVALID_TEMPLATE_MSG}";
                return;
            }

            CanApply = true;
            WarningMessage = string.Empty;
        }

        private static bool HasAtLeastOneTag(string value)
        {
            return SupportedTags.Any(tag => value.Contains(tag, StringComparison.Ordinal));
        }

        private bool HasAllRequiredTags(string value)
        {
            if (MinTagsRequiredIndex != 1)
            {
                return true;
            }

            foreach (string tag in GetRequiredTags())
            {
                if (!value.Contains(tag, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<string> GetRequiredTags()
        {
            if (TrackNumRequired)
            {
                yield return "<TrackNum>";
            }

            if (TitleRequired)
            {
                yield return "<Title>";
            }

            if (AlbumRequired)
            {
                yield return "<Album>";
            }

            if (AlbumArtistRequired)
            {
                yield return "<AlbumArtist>";
            }

            if (ArtistRequired)
            {
                yield return "<Artist>";
            }

            if (YearRequired)
            {
                yield return "<Year>";
            }
        }
    }
}
