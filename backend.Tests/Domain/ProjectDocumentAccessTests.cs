using KnowledgeBank.Models.ProjectDocuments;
using KnowledgeBank.Services.Domain;

namespace KnowledgeBank.Tests.Domain;

public class ProjectDocumentAccessTests
{
    private const string Uploader = "uploader";
    private const string User = "user";

    private static readonly ProjectDocumentAccessType Viewer = ProjectDocumentAccessType.Viewer;
    private static readonly ProjectDocumentAccessType Editor = ProjectDocumentAccessType.Editor;

    private static ProjectDocument Document(ProjectDocumentVisibility visibility, ProjectDocumentAccessType general, ProjectDocumentAccessType? grant = null) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Title = "Document",
        TypeId = Guid.NewGuid(),
        Visibility = visibility,
        GeneralAccessType = general,
        CreatedBy = Uploader,
        CreatedOn = DateTime.UtcNow,
        UpdatedOn = DateTime.UtcNow,
        Access = grant == null ? [] : [new ProjectDocumentAccess { DocumentId = Guid.Empty, UserId = User, AccessType = grant.Value }]
    };

    private static ProjectDocumentAccessType? Level(ProjectDocument document, bool isMember = false, bool isOwner = false, string userId = User)
        => ProjectDocumentService.GetAccessLevel(document, userId, isMember, isOwner);

    // General access

    [Theory]
    [InlineData(ProjectDocumentAccessType.Viewer)]
    [InlineData(ProjectDocumentAccessType.Editor)]
    public void Everyone_GivesEveryUserTheGeneralLevel(ProjectDocumentAccessType general)
        => Assert.Equal(general, Level(Document(ProjectDocumentVisibility.Everyone, general)));

    [Theory]
    [InlineData(ProjectDocumentAccessType.Viewer)]
    [InlineData(ProjectDocumentAccessType.Editor)]
    public void ProjectMembers_GivesMembersTheGeneralLevel(ProjectDocumentAccessType general)
        => Assert.Equal(general, Level(Document(ProjectDocumentVisibility.ProjectMembers, general), isMember: true));

    [Fact]
    public void ProjectMembers_GivesNonMembersNothing()
        => Assert.Null(Level(Document(ProjectDocumentVisibility.ProjectMembers, Editor)));

    [Fact]
    public void Restricted_GivesEvenMembersNothing()
        => Assert.Null(Level(Document(ProjectDocumentVisibility.Restricted, Editor), isMember: true));

    // Personal grants combine with general access to the highest of the two

    [Theory]
    [InlineData(ProjectDocumentAccessType.Viewer)]
    [InlineData(ProjectDocumentAccessType.Editor)]
    public void Restricted_GivesGrantedUsersTheirGrant(ProjectDocumentAccessType grant)
        => Assert.Equal(grant, Level(Document(ProjectDocumentVisibility.Restricted, Editor, grant)));

    [Fact]
    public void Grant_RaisesEveryoneViewerToEditor()
        => Assert.Equal(Editor, Level(Document(ProjectDocumentVisibility.Everyone, Viewer, Editor)));

    [Fact]
    public void Grant_RaisesMemberViewerToEditor()
        => Assert.Equal(Editor, Level(Document(ProjectDocumentVisibility.ProjectMembers, Viewer, Editor), isMember: true));

    [Fact]
    public void Grant_NeverLowersGeneralAccess()
        => Assert.Equal(Editor, Level(Document(ProjectDocumentVisibility.ProjectMembers, Editor, Viewer), isMember: true));

    [Fact]
    public void Grant_GivesNonMembersAccessToAMembersDocument()
        => Assert.Equal(Viewer, Level(Document(ProjectDocumentVisibility.ProjectMembers, Editor, Viewer)));

    [Fact]
    public void Grant_OnlyAppliesToThatUser()
        => Assert.Null(Level(Document(ProjectDocumentVisibility.Restricted, Editor, Editor), userId: "someone-else"));

    // Always editors

    [Fact]
    public void Uploader_IsAlwaysEditor()
        => Assert.Equal(Editor, Level(Document(ProjectDocumentVisibility.Restricted, Viewer), userId: Uploader));

    [Fact]
    public void Owner_IsAlwaysEditor()
        => Assert.Equal(Editor, Level(Document(ProjectDocumentVisibility.Restricted, Viewer), isOwner: true));
}
