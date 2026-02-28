using RenameMusic.Models;
using RenameMusic.Resources.Languages;

namespace RenameMusic.Services
{
    /// <summary>
    /// Converts persisted reason payloads to localized UI strings.
    /// </summary>
    public static class NotRenamableReasonTextService
    {
        public static string ToDisplayText(string? reasonPayload)
        {
            NotRenamableReasonData reason = NotRenamableReasonCodec.Parse(reasonPayload);
            return reason.Code switch
            {
                NotRenamableReasonCodes.FileNotFound => Strings.FILE_NOT_FOUND_MSG,
                NotRenamableReasonCodes.UnreadableMetadata => L("REASON_UNREADABLE_METADATA", "Unreadable metadata."),
                NotRenamableReasonCodes.EmptyProposedFileName => L("REASON_EMPTY_PROPOSED_FILE_NAME", "Empty proposed file name."),
                NotRenamableReasonCodes.TemplateRequiresTag => L("REASON_TEMPLATE_REQUIRES_TAG", "Template must include at least one metadata tag."),
                NotRenamableReasonCodes.MissingRequiredTags => BuildMessage(
                    L("REASON_MISSING_REQUIRED_TAGS_FORMAT", "Missing required tags: {0}"),
                    reason.Detail),
                NotRenamableReasonCodes.TemplateProducedEmptyName => L("REASON_TEMPLATE_EMPTY_NAME", "Template produced an empty file name."),
                NotRenamableReasonCodes.ManuallyExcluded => L("REASON_MANUALLY_EXCLUDED", "Manually excluded."),
                NotRenamableReasonCodes.GenericError => BuildMessage(
                    L("REASON_GENERIC_ERROR_FORMAT", "Error: {0}"),
                    reason.Detail),
                _ => string.IsNullOrWhiteSpace(reason.Detail)
                    ? L("REASON_UNKNOWN", "Unknown reason.")
                    : reason.Detail
            };
        }

        private static string BuildMessage(string format, string? detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return string.Format(format, string.Empty).Trim().TrimEnd(':').Trim();
            }

            return string.Format(format, detail);
        }

        private static string L(string key, string fallback)
        {
            return Strings.ResourceManager.GetString(key, Strings.Culture) ?? fallback;
        }
    }
}
