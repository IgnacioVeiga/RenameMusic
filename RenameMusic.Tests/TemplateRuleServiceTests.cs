using RenameMusic.Models;
using RenameMusic.Services;

namespace RenameMusic.Tests
{
    public sealed class TemplateRuleServiceTests
    {
        private readonly TemplateRuleService _service = new();

        [Fact]
        public void Evaluate_ShouldFail_WhenTemplateHasNoSupportedTags()
        {
            SessionAudioEntity entity = CreateEntity();
            RenameRuleOptions options = CreateOptions(
                "StaticName",
                MissingTagStrategy.Strict,
                2,
                new HashSet<string>(StringComparer.Ordinal));

            RuleEvaluationResult result = _service.Evaluate(entity, options);

            Assert.False(result.CanRename);
            NotRenamableReasonData reason = NotRenamableReasonCodec.Parse(result.Reason);
            Assert.Equal(NotRenamableReasonCodes.TemplateRequiresTag, reason.Code);
        }

        [Fact]
        public void Evaluate_ShouldRequireAllMentionedTags_WhenModeIsAllMentioned()
        {
            SessionAudioEntity entity = CreateEntity(title: null, album: "Test Album");
            RenameRuleOptions options = CreateOptions(
                "<Title> - <Album>",
                MissingTagStrategy.Strict,
                2,
                new HashSet<string>(StringComparer.Ordinal) { "<Title>" });

            RuleEvaluationResult result = _service.Evaluate(entity, options);

            Assert.False(result.CanRename);
            Assert.Contains("<Title>", result.MissingTokens);
            NotRenamableReasonData reason = NotRenamableReasonCodec.Parse(result.Reason);
            Assert.Equal(NotRenamableReasonCodes.MissingRequiredTags, reason.Code);
            Assert.Contains("<Title>", reason.Detail);
        }

        [Fact]
        public void Evaluate_ShouldAllowMissingTags_WhenModeIsNoneRequired()
        {
            SessionAudioEntity entity = CreateEntity(title: null, album: "Test Album");
            RenameRuleOptions options = CreateOptions(
                "<Title> - <Album>",
                MissingTagStrategy.Strict,
                0,
                new HashSet<string>(StringComparer.Ordinal) { "<Title>" });

            RuleEvaluationResult result = _service.Evaluate(entity, options);

            Assert.True(result.CanRename);
            Assert.Equal("- Test Album", result.ProposedName);
        }

        [Fact]
        public void Evaluate_ShouldRequireOnlyMarkedTokens_WhenModeIsOnlyMarked()
        {
            SessionAudioEntity entity = CreateEntity(title: "Song", album: null);
            RenameRuleOptions options = CreateOptions(
                "<Title> - <Album>",
                MissingTagStrategy.Strict,
                1,
                new HashSet<string>(StringComparer.Ordinal) { "<Title>" });

            RuleEvaluationResult result = _service.Evaluate(entity, options);

            Assert.True(result.CanRename);
            Assert.Equal("Song -", result.ProposedName);
        }

        [Fact]
        public void Evaluate_ShouldUsePlaceholder_WhenRequiredTagMissingAndPlaceholderMode()
        {
            SessionAudioEntity entity = CreateEntity(title: null, album: "Test Album");
            RenameRuleOptions options = CreateOptions(
                "<Title> - <Album>",
                MissingTagStrategy.UsePlaceholder,
                2,
                new HashSet<string>(StringComparer.Ordinal) { "<Title>" });

            RuleEvaluationResult result = _service.Evaluate(entity, options);

            Assert.True(result.CanRename);
            Assert.Equal("Unknown - Test Album", result.ProposedName);
        }

        [Fact]
        public void Evaluate_ShouldNotReplaceTagLikeTextInsideMetadataValue()
        {
            SessionAudioEntity entity = CreateEntity(title: "Live <Album>", album: "Greatest");
            RenameRuleOptions options = CreateOptions(
                "<Title> - <Album>",
                MissingTagStrategy.Strict,
                2,
                new HashSet<string>(StringComparer.Ordinal));

            RuleEvaluationResult result = _service.Evaluate(entity, options);

            Assert.True(result.CanRename);
            Assert.Equal("Live _Album_ - Greatest", result.ProposedName);
        }

        private static RenameRuleOptions CreateOptions(
            string template,
            MissingTagStrategy strategy,
            int minTagsRequiredIndex,
            IReadOnlySet<string> requiredTokens)
        {
            return new RenameRuleOptions
            {
                Template = template,
                MissingTagStrategy = strategy,
                PlaceholderText = "Unknown",
                MinTagsRequiredIndex = minTagsRequiredIndex,
                RequiredTokens = requiredTokens
            };
        }

        private static SessionAudioEntity CreateEntity(string? title = "Song", string? album = "Album")
        {
            return new SessionAudioEntity
            {
                Id = 1,
                FullPath = @"C:\Music\song.mp3",
                FolderPath = @"C:\Music\",
                FileNameWithoutExtension = "song",
                FileExtension = ".mp3",
                DurationSeconds = 120,
                TrackNum = 1,
                Title = title,
                Album = album,
                AlbumArtist = "Album Artist",
                Artist = "Artist",
                Year = 2024,
                CanRename = true,
                ExistsOnDisk = true
            };
        }
    }
}
