using RenameMusic.Models;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Applies a rename template to metadata entities and determines whether each file is eligible to be renamed.
    /// </summary>
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

        private static readonly Regex SupportedTokenRegex = new(
            @"<TrackNum>|<Title>|<Album>|<AlbumArtist>|<Artist>|<Year>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        /// <summary>
        /// Evaluates one audio item against the active template and required-tag policy.
        /// </summary>
        public RuleEvaluationResult Evaluate(SessionAudioEntity entity, RenameRuleOptions options)
        {
            IReadOnlyList<string> usedTokens = GetUsedTokens(options.Template);
            if (usedTokens.Count == 0)
            {
                return new RuleEvaluationResult
                {
                    CanRename = false,
                    Reason = NotRenamableReasonCodec.Create(NotRenamableReasonCodes.TemplateRequiresTag)
                };
            }

            HashSet<string> requiredTokens = ResolveRequiredTokens(usedTokens, options);
            Dictionary<string, string> replacements = new(StringComparer.Ordinal);
            List<string> missingRequiredTokens = [];
            foreach (string token in usedTokens)
            {
                string? value = GetTokenValue(entity, token);
                bool missing = string.IsNullOrWhiteSpace(value);
                if (missing)
                {
                    if (requiredTokens.Contains(token))
                    {
                        missingRequiredTokens.Add(token);
                        value = options.MissingTagStrategy == MissingTagStrategy.Strict
                            ? string.Empty
                            : options.PlaceholderText;
                    }
                    else
                    {
                        // Optional tags can be omitted from the final filename.
                        value = string.Empty;
                    }
                }

                replacements[token] = value ?? string.Empty;
            }

            if (missingRequiredTokens.Count > 0 && options.MissingTagStrategy == MissingTagStrategy.Strict)
            {
                return new RuleEvaluationResult
                {
                    CanRename = false,
                    Reason = NotRenamableReasonCodec.Create(
                        NotRenamableReasonCodes.MissingRequiredTags,
                        string.Join(", ", missingRequiredTokens)),
                    MissingTokens = missingRequiredTokens
                };
            }

            string proposedName = ReplaceTokens(options.Template, replacements);
            proposedName = FilenameFunctions.NormalizeFileName(proposedName).Trim();
            if (string.IsNullOrWhiteSpace(proposedName))
            {
                return new RuleEvaluationResult
                {
                    CanRename = false,
                    Reason = NotRenamableReasonCodec.Create(NotRenamableReasonCodes.TemplateProducedEmptyName)
                };
            }

            return new RuleEvaluationResult
            {
                CanRename = true,
                ProposedName = proposedName,
                MissingTokens = missingRequiredTokens
            };
        }

        private static HashSet<string> ResolveRequiredTokens(
            IReadOnlyList<string> usedTokens,
            RenameRuleOptions options)
        {
            return options.MinTagsRequiredIndex switch
            {
                0 => new HashSet<string>(StringComparer.Ordinal),
                1 => new HashSet<string>(
                    usedTokens.Where(options.RequiredTokens.Contains),
                    StringComparer.Ordinal),
                _ => new HashSet<string>(usedTokens, StringComparer.Ordinal)
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

        private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> replacements)
        {
            return SupportedTokenRegex.Replace(
                template,
                match => replacements.TryGetValue(match.Value, out string? replacement)
                    ? replacement
                    : string.Empty);
        }
    }
}
