namespace RenameMusic.Models
{
    /// <summary>
    /// Stable reason codes persisted in the session store for "Do Not Rename" items.
    /// </summary>
    public static class NotRenamableReasonCodes
    {
        public const string Unknown = "UNKNOWN";
        public const string FileNotFound = "FILE_NOT_FOUND";
        public const string UnreadableMetadata = "UNREADABLE_METADATA";
        public const string EmptyProposedFileName = "EMPTY_PROPOSED_FILE_NAME";
        public const string TemplateRequiresTag = "TEMPLATE_REQUIRES_TAG";
        public const string MissingRequiredTags = "MISSING_REQUIRED_TAGS";
        public const string TemplateProducedEmptyName = "TEMPLATE_PRODUCED_EMPTY_NAME";
        public const string ManuallyExcluded = "MANUALLY_EXCLUDED";
        public const string GenericError = "GENERIC_ERROR";
    }

    /// <summary>
    /// Encodes and decodes persisted reason payloads as "CODE|detail".
    /// </summary>
    public static class NotRenamableReasonCodec
    {
        private const char DetailSeparator = '|';

        public static string Create(string code, string? detail = null)
        {
            string normalizedCode = NormalizeCode(code);
            if (string.IsNullOrWhiteSpace(detail))
            {
                return normalizedCode;
            }

            return $"{normalizedCode}{DetailSeparator}{detail}";
        }

        public static NotRenamableReasonData Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new NotRenamableReasonData(NotRenamableReasonCodes.Unknown, null);
            }

            string trimmed = payload.Trim();
            if (TryParseEncoded(trimmed, out NotRenamableReasonData encoded))
            {
                return encoded;
            }

            return new NotRenamableReasonData(NotRenamableReasonCodes.GenericError, trimmed);
        }

        private static bool TryParseEncoded(string payload, out NotRenamableReasonData parsed)
        {
            int separatorIndex = payload.IndexOf(DetailSeparator);
            if (separatorIndex < 0)
            {
                string code = NormalizeCode(payload);
                if (IsKnownCode(code))
                {
                    parsed = new NotRenamableReasonData(code, null);
                    return true;
                }

                parsed = default;
                return false;
            }

            string codePart = payload[..separatorIndex];
            string detailPart = payload[(separatorIndex + 1)..];
            string normalizedCode = NormalizeCode(codePart);
            if (!IsKnownCode(normalizedCode))
            {
                parsed = default;
                return false;
            }

            parsed = new NotRenamableReasonData(
                normalizedCode,
                string.IsNullOrWhiteSpace(detailPart) ? null : detailPart);
            return true;
        }

        private static bool IsKnownCode(string code)
        {
            return code == NotRenamableReasonCodes.Unknown
                || code == NotRenamableReasonCodes.FileNotFound
                || code == NotRenamableReasonCodes.UnreadableMetadata
                || code == NotRenamableReasonCodes.EmptyProposedFileName
                || code == NotRenamableReasonCodes.TemplateRequiresTag
                || code == NotRenamableReasonCodes.MissingRequiredTags
                || code == NotRenamableReasonCodes.TemplateProducedEmptyName
                || code == NotRenamableReasonCodes.ManuallyExcluded
                || code == NotRenamableReasonCodes.GenericError;
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return NotRenamableReasonCodes.Unknown;
            }

            return code.Trim().ToUpperInvariant();
        }
    }

    public readonly record struct NotRenamableReasonData(string Code, string? Detail);
}
