using System.Text.Json;
using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class ChatToolArgsTests
{
    [Theory]
    [InlineData("""{"tagNames":["Solar","Wind"]}""", new[] { "Solar", "Wind" })]
    [InlineData("""{"tagNames":["Solar","  ","",null,"Wind"]}""", new[] { "Solar", "Wind" })]
    [InlineData("""{"tagNames":[]}""", new string[0])]
    [InlineData("""{"tagNames":"Solar"}""", new string[0])]
    [InlineData("""{"other":["Solar"]}""", new string[0])]
    public void GetStringArray_ReturnsNonBlankStrings(string json, string[] expected)
    {
        using JsonDocument args = JsonDocument.Parse(json);
        Assert.Equal(expected, ChatToolExecutor.GetStringArray(args, "tagNames"));
    }
}
