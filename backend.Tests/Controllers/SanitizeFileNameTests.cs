using KnowledgeBank.Controllers;

namespace KnowledgeBank.Tests.Controllers;

public class SanitizeFileNameTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_BecomesUnnamed(string name)
        => Assert.Equal("unnamed", FilesController.SanitizeFileName(name));

    [Fact]
    public void PathSeparators_AreReplaced()
        => Assert.Equal("reports_2024_plan.pdf", FilesController.SanitizeFileName("reports/2024/plan.pdf"));

    [Fact]
    public void TypographicCharacters_AreReplaced()
        => Assert.Equal("_Energy_ report _ final_.pdf", FilesController.SanitizeFileName("“Energy” report — final….pdf"));

    [Fact]
    public void Spaces_AreKeptByDefault_OrReplacedOnRequest()
    {
        Assert.Equal("project plan.pdf", FilesController.SanitizeFileName("project plan.pdf"));
        Assert.Equal("project_plan.pdf", FilesController.SanitizeFileName("project plan.pdf", preserveSpaces: false));
    }

    [Theory]
    [InlineData(".env", "_env")]
    [InlineData("..hidden.pdf", "_hidden.pdf")]
    public void LeadingDots_AreReplaced(string name, string expected)
        => Assert.Equal(expected, FilesController.SanitizeFileName(name));

    [Theory]
    [InlineData(255, 255)]
    [InlineData(256, 255)]
    [InlineData(400, 255)]
    public void LongNames_AreCappedAt255Characters(int inputLength, int expectedLength)
        => Assert.Equal(expectedLength, FilesController.SanitizeFileName(new string('a', inputLength)).Length);
}
