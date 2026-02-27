using RenameMusic.Models;

namespace RenameMusic.Services
{
    public interface ITemplateRuleService
    {
        IReadOnlyList<string> GetUsedTokens(string template);
        IReadOnlyList<string> GetRepeatedTokens(string template);
        RuleEvaluationResult Evaluate(SessionAudioEntity entity, RenameRuleOptions options);
    }

    public sealed class RuleEvaluationResult
    {
        public bool CanRename { get; init; }
        public string? ProposedName { get; init; }
        public string? Reason { get; init; }
        public IReadOnlyList<string> MissingTokens { get; init; } = [];
    }

    public sealed class TemplateRuleService : ITemplateRuleService
    {
        private static readonly string[] SupportedTokens =
        [
            "<TrackNum>",
            "<Title>",
            "<Album>",
            "<AlbumArtist>",
            "<Artist>",
            "<Year>"
        ];

        public IReadOnlyList<string> GetUsedTokens(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return [];
            }

            return SupportedTokens.Where(template.Contains).ToArray();
        }

        public IReadOnlyList<string> GetRepeatedTokens(string template)
        {
            List<string> repeated = [];
            foreach (string token in SupportedTokens)
            {
                int first = template.IndexOf(token, StringComparison.Ordinal);
                if (first < 0)
                {
                    continue;
                }

                int second = template.IndexOf(token, first + token.Length, StringComparison.Ordinal);
                if (second >= 0)
                {
                    repeated.Add(token);
                }
            }

            return repeated;
        }

        public RuleEvaluationResult Evaluate(SessionAudioEntity entity, RenameRuleOptions options)
        {
            IReadOnlyList<string> usedTokens = GetUsedTokens(options.Template);
            if (usedTokens.Count == 0)
            {
                return new RuleEvaluationResult
                {
                    CanRename = false,
                    Reason = "Template must include at least one metadata tag."
                };
            }

            string proposedName = options.Template;
            List<string> missingTokens = [];
            foreach (string token in usedTokens)
            {
                string? value = GetTokenValue(entity, token);
                bool missing = string.IsNullOrWhiteSpace(value);
                if (missing)
                {
                    missingTokens.Add(token);
                    if (options.MissingTagStrategy == MissingTagStrategy.Strict)
                    {
                        continue;
                    }

                    value = options.PlaceholderText;
                }

                proposedName = proposedName.Replace(token, value ?? string.Empty, StringComparison.Ordinal);
            }

            if (missingTokens.Count > 0 && options.MissingTagStrategy == MissingTagStrategy.Strict)
            {
                return new RuleEvaluationResult
                {
                    CanRename = false,
                    Reason = $"Missing required tags: {string.Join(", ", missingTokens)}",
                    MissingTokens = missingTokens
                };
            }

            proposedName = FilenameFunctions.NormalizeFileName(proposedName).Trim();
            if (string.IsNullOrWhiteSpace(proposedName))
            {
                return new RuleEvaluationResult
                {
                    CanRename = false,
                    Reason = "Template produced an empty file name."
                };
            }

            return new RuleEvaluationResult
            {
                CanRename = true,
                ProposedName = proposedName,
                MissingTokens = missingTokens
            };
        }

        private static string? GetTokenValue(SessionAudioEntity entity, string token)
        {
            return token switch
            {
                "<TrackNum>" => entity.TrackNum is > 0 ? entity.TrackNum.Value.ToString() : null,
                "<Title>" => entity.Title,
                "<Album>" => entity.Album,
                "<AlbumArtist>" => entity.AlbumArtist,
                "<Artist>" => entity.Artist,
                "<Year>" => entity.Year is > 0 ? entity.Year.Value.ToString() : null,
                _ => null
            };
        }
    }
}
