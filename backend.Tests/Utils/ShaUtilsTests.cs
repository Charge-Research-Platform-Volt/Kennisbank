using KnowledgeBank.Utils;

namespace KnowledgeBank.Tests.Utils;

public class ShaUtilsTests
{
    [Theory]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    public void Sha256_MatchesKnownHashes(string input, string expected)
        => Assert.Equal(expected, ShaUtils.Sha256(input));
}
