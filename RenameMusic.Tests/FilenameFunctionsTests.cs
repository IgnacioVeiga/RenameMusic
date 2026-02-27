namespace RenameMusic.Tests
{
    public sealed class FilenameFunctionsTests
    {
        [Fact]
        public void NormalizeFileName_ShouldReplaceInvalidCharacters()
        {
            string rawName = "Title Song/Artist";

            string normalized = FilenameFunctions.NormalizeFileName(rawName);

            Assert.DoesNotContain('/', normalized);
        }

        [Fact]
        public void NormalizeFileName_ShouldReplaceTrailingDot()
        {
            string rawName = "Track 01.";

            string normalized = FilenameFunctions.NormalizeFileName(rawName);

            Assert.Equal("Track 01_", normalized);
        }
    }
}
