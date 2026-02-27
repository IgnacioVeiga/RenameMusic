namespace RenameMusic.Models
{
    public enum MissingTagStrategy
    {
        Strict = 0,
        UsePlaceholder = 1
    }

    public enum ConflictResolutionAction
    {
        Replace = 0,
        Skip = 1,
        RenameWithNumber = 2
    }

    public sealed class RenameRuleOptions
    {
        public required string Template { get; init; }
        public required MissingTagStrategy MissingTagStrategy { get; init; }
        public required string PlaceholderText { get; init; }
        public required int MinTagsRequiredIndex { get; init; }
        public required IReadOnlySet<string> RequiredTokens { get; init; }
    }

    public sealed class TemplateDialogResult
    {
        public required string Template { get; init; }
        public int MinTagsRequiredIndex { get; init; }
        public bool TrackNumRequired { get; init; }
        public bool TitleRequired { get; init; }
        public bool AlbumRequired { get; init; }
        public bool AlbumArtistRequired { get; init; }
        public bool ArtistRequired { get; init; }
        public bool YearRequired { get; init; }
    }

    public sealed class ConflictDialogResult
    {
        public required ConflictResolutionAction Action { get; init; }
        public bool ApplyToAll { get; init; }
    }
}
