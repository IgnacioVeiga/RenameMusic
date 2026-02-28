using RenameMusic.Models;

namespace RenameMusic.Tests
{
    public sealed class NotRenamableReasonCodecTests
    {
        [Fact]
        public void Parse_ShouldDecode_EncodedReasonWithDetail()
        {
            string payload = NotRenamableReasonCodec.Create(
                NotRenamableReasonCodes.MissingRequiredTags,
                "<Title>, <TrackNum>");

            NotRenamableReasonData reason = NotRenamableReasonCodec.Parse(payload);

            Assert.Equal(NotRenamableReasonCodes.MissingRequiredTags, reason.Code);
            Assert.Equal("<Title>, <TrackNum>", reason.Detail);
        }

        [Fact]
        public void Parse_ShouldFallbackTo_GenericError_ForUnknownPayload()
        {
            NotRenamableReasonData reason = NotRenamableReasonCodec.Parse("unexpected-runtime-error");

            Assert.Equal(NotRenamableReasonCodes.GenericError, reason.Code);
            Assert.Equal("unexpected-runtime-error", reason.Detail);
        }
    }
}
