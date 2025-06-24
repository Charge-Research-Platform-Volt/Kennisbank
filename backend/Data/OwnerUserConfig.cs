namespace KnowledgeBank.Data;

public class OwnerUserConfig
{
    public const string SectionName = "OwnerUser";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Owner";
    public string LastName { get; set; } = "Owner";
}