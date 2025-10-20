namespace KnowledgeBank.Data;

public class OwnerUserConfig
{
    public const string SectionName = "OwnerUser";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Owner";
    public string LastName { get; set; } = "Owner";
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


