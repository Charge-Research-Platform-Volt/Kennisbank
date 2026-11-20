using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class ChatToolDefinitionsTests
{
    private static readonly string[] DocumentToolNames = ["list_project_documents", "search_project_documents", "read_project_document"];

    [Fact]
    public void ALibraryChat_GetsNoProjectDocumentTools()
    {
        string[] names = [.. ChatToolDefinitions.ForScope(inProject: false).Select(t => t.Name)];

        Assert.Empty(names.Intersect(DocumentToolNames));
        Assert.Contains("search_library", names);
    }

    [Fact]
    public void AProjectChat_GetsTheLibraryAndTheProjectDocumentTools()
    {
        string[] names = [.. ChatToolDefinitions.ForScope(inProject: true).Select(t => t.Name)];

        Assert.Superset(new HashSet<string>(DocumentToolNames), new HashSet<string>(names));
        Assert.Superset(new HashSet<string>(ChatToolDefinitions.Library.Select(t => t.Name)), new HashSet<string>(names));
    }

    [Fact]
    public void ToolNames_AreUnique()
    {
        // The executor dispatches on the name, and the usage metrics only count known names
        string[] names = [.. ChatToolDefinitions.All.Select(t => t.Name)];
        Assert.Equal(names.Length, names.Distinct().Count());
    }
}
