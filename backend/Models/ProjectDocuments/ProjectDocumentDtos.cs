namespace KnowledgeBank.Models.ProjectDocuments;

public class ProjectDocumentGrantDto
{
    public required Guid UserId { get; set; }
    public required ProjectDocumentAccessType AccessType { get; set; }
}

public class ProjectDocumentCreateDto
{
    public required string ObjectName { get; set; }
    public required string FileName { get; set; }
    public required string Title { get; set; }
    public required Guid TypeId { get; set; }
    public ProjectDocumentVisibility Visibility { get; set; } = ProjectDocumentVisibility.ProjectMembers;
    public ProjectDocumentAccessType GeneralAccessType { get; set; } = ProjectDocumentAccessType.Editor;
    public ProjectDocumentGrantDto[] Grants { get; set; } = [];
    public string? Note { get; set; }
}

public class ProjectDocumentVersionCreateDto
{
    public required string ObjectName { get; set; }
    public required string FileName { get; set; }
    public string? Note { get; set; }
}

public class ProjectDocumentUpdateDto
{
    public string? Title { get; set; }
    public Guid? TypeId { get; set; }
}

public class ProjectDocumentAccessUpdateDto
{
    public required ProjectDocumentVisibility Visibility { get; set; }
    public required ProjectDocumentAccessType GeneralAccessType { get; set; }
    public ProjectDocumentGrantDto[] Grants { get; set; } = [];
}

public class ProjectDocumentTypeCreateDto
{
    public required string Name { get; set; }
}

public class ProjectDocumentVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public string? Note { get; set; }
    public Guid UploadedBy { get; set; }
    public string UploadedByName { get; set; } = "Unknown";
    public DateTime UploadedOn { get; set; }
    public EmbeddingStatus PreviewStatus { get; set; }
    public EmbeddingStatus EmbeddingStatus { get; set; }
}

public class ProjectDocumentGrantInfoDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? CustomAvatarVersion { get; set; }
    public ProjectDocumentAccessType AccessType { get; set; }
}

public class ProjectDocumentListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public ProjectDocumentVisibility Visibility { get; set; }
    public ProjectDocumentAccessType GeneralAccessType { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public ProjectDocumentVersionDto LatestVersion { get; set; } = null!;

    // Th caller's own level, so the frontend knows which actions to show
    public ProjectDocumentAccessType AccessLevel { get; set; }
}

public class ProjectDocumentDetailDto : ProjectDocumentListItemDto
{
    public List<ProjectDocumentVersionDto> Versions { get; set; } = [];
    public List<ProjectDocumentGrantInfoDto> Grants { get; set; } = [];
}