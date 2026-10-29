using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Models.ProjectDocuments;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgeBank.Services.Domain;

public class ProjectDocumentService(DatabaseContext db, IStorageService storageService, EnvironmentConfig environmentConfig)
{
    // Keeps these files out of reach of /files/{id}, which serves any GUID key without access checks
    public const string StoragePrefix = "project-docs/";
    public static readonly string[] SupportedExtensions = ["pdf", "docx"];

    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);
    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<ProjectDocumentService>();

    public static string StorageKey(Guid versionId) => $"{StoragePrefix}{versionId}";
    public static string PreviewKey(Guid versionId) => $"{StoragePrefix}{versionId}-preview";
    public static string OcrCacheKey(Guid versionId) => $"{StoragePrefix}{versionId}-ocr";

    #region Access

    /// <summary>
    /// The single source of truth for document access. Returns null when the user has no access.
    /// The general access and a personal grant combine to the highest of the two; a grant never lowers access.
    /// </summary>
    internal static ProjectDocumentAccessType? GetAccessLevel(ProjectDocument document, string userId, bool isProjectMember, bool isOwner)
    {
        // The owner account can always recover a document, and the uploader keeps control of their own upload
        if (isOwner || document.CreatedBy == userId) return ProjectDocumentAccessType.Editor;

        ProjectDocumentAccessType? general = document.Visibility switch
        {
            ProjectDocumentVisibility.Everyone => document.GeneralAccessType,
            ProjectDocumentVisibility.ProjectMembers when isProjectMember => document.GeneralAccessType,
            _ => null
        };

        ProjectDocumentAccessType? grant = document.Access?.FirstOrDefault(a => a.UserId == userId)?.AccessType;

        if (general == null) return grant;
        if (grant == null) return general;
        return general > grant ? general : grant;
    }

    public async Task<bool> IsMemberAsync(Guid rootProjectId, string userId)
        => await db.ProjectMemberRelations.AnyAsync(r => r.ProjectId == rootProjectId && r.UserId == userId);

    public async Task<Guid[]> GetViewableDocumentIdsAsync(Guid rootProjectId, string userId, bool isOwner)
    {
        bool isMember = await IsMemberAsync(rootProjectId, userId);
        List<ProjectDocument> documents = await db.ProjectDocuments
            .AsNoTracking()
            .Include(d => d.Access)
            .Where(d => d.ProjectId == rootProjectId)
            .ToListAsync();

        return [.. documents.Where(d => GetAccessLevel(d, userId, isMember, isOwner) != null).Select(d => d.Id)];
    }

    #endregion

    #region Queries

    /// <summary>Loads a document with everything the access check and detail view need. Tracked, so it can be modified.</summary>
    public async Task<ProjectDocument?> GetAsync(Guid rootProjectId, Guid documentId)
        => await db.ProjectDocuments
            .Include(d => d.Access)
            .Include(d => d.Versions)
            .Include(d => d.Type)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ProjectId == rootProjectId);

    public async Task<List<ProjectDocumentListItemDto>> ListAsync(Guid rootProjectId, string userId, bool isOwner)
    {
        bool isMember = await IsMemberAsync(rootProjectId, userId);
        List<ProjectDocument> documents = await db.ProjectDocuments
            .AsNoTracking()
            .Include(d => d.Access)
            .Include(d => d.Versions)
            .Include(d => d.Type)
            .Where(d => d.ProjectId == rootProjectId)
            .OrderByDescending(d => d.UpdatedOn)
            .ToListAsync();

        var visible = documents
            .Select(d => (Document: d, Level: GetAccessLevel(d, userId, isMember, isOwner)))
            .Where(x => x.Level != null)
            .ToList();

        Dictionary<string, string> names = await GetUserNamesAsync(visible.Select(x => LatestVersion(x.Document).UploadedBy));

        return [.. visible.Select(x => ToListItem(new ProjectDocumentListItemDto(), x.Document, x.Level!.Value, names))];
    }

    public async Task<ProjectDocumentDetailDto> GetDetailAsync(ProjectDocument document, ProjectDocumentAccessType level)
    {
        Dictionary<string, string> names = await GetUserNamesAsync(document.Versions!.Select(v => v.UploadedBy));

        string[] grantUserIds = [.. document.Access!.Select(a => a.UserId)];
        var grantUsers = await db.Users
            .AsNoTracking()
            .Where(u => grantUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.HasCustom, u.CustomAvatarVersion })
            .ToDictionaryAsync(u => u.Id);

        ProjectDocumentDetailDto detail = ToListItem(new ProjectDocumentDetailDto(), document, level, names);
        detail.Versions = [.. document.Versions!.OrderByDescending(v => v.VersionNumber).Select(v => ToVersionDto(v, names))];
        detail.Grants = [.. document.Access!
            .Where(a => grantUsers.ContainsKey(a.UserId))
            .Select(a => new ProjectDocumentGrantInfoDto {
                UserId = Guid.Parse(a.UserId),
                FirstName = grantUsers[a.UserId].FirstName,
                LastName = grantUsers[a.UserId].LastName,
                CustomAvatarVersion = grantUsers[a.UserId].HasCustom ? grantUsers[a.UserId].CustomAvatarVersion : null,
                AccessType = a.AccessType
            })];

        return detail;
    }

    /// <summary>
    /// True when objectId is a finished project document upload that no version uses yet. Uploads land under the
    /// project-docs prefix directly.
    /// </summary>
    public async Task<bool> IsUploadAvailableAsync(Guid objectId)
    {
        if (await db.ProjectDocumentVersions.AnyAsync(v => v.Id == objectId)) return false;

        string key = StorageKey(objectId);
        StorageObjectInfo[] objects = await storageService.ListObjectsAsync(bucketName, key);
        return objects.Any(o => o.Key == key);
    }

    public async Task<bool> AllUsersActiveAsync(IEnumerable<Guid> userIds)
    {
        string[] ids = [.. userIds.Select(id => id.ToString()).Distinct()];
        return await db.Users.CountAsync(u => ids.Contains(u.Id) && !u.IsDeleted) == ids.Length;
    }

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(Guid rootProjectId, ProjectDocumentCreateDto dto, string userId)
    {
        Guid documentId = Guid.NewGuid();
        // The upload already sits at project-docs/{objectName}, so it becomes the first version as is
        Guid versionId = Guid.Parse(dto.ObjectName);
        DateTime now = DateTime.UtcNow;

        db.ProjectDocuments.Add(new ProjectDocument
        {
            Id = documentId,
            ProjectId = rootProjectId,
            Title = dto.Title.Trim(),
            TypeId = dto.TypeId,
            Visibility = dto.Visibility,
            GeneralAccessType = dto.GeneralAccessType,
            CreatedBy = userId,
            CreatedOn = now,
            UpdatedOn = now,
            Versions = [NewVersion(versionId, documentId, 1, dto.FileName, dto.Note, userId, now)],
            Access = [.. dto.Grants.Select(g => NewGrant(documentId, g))]
        });

        await db.SaveChangesAsync();
        return documentId;
    }

    /// <summary>Returns the new version's id, or null when someone else saved a version with the same number first.</summary>
    public async Task<Guid?> AddVersionAsync(ProjectDocument document, ProjectDocumentVersionCreateDto dto, string userId)
    {
        Guid versionId = Guid.Parse(dto.ObjectName);
        DateTime now = DateTime.UtcNow;
        int versionNumber = (await db.ProjectDocumentVersions
            .Where(v => v.DocumentId == document.Id)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0) + 1;

        db.ProjectDocumentVersions.Add(NewVersion(versionId, document.Id, versionNumber, dto.FileName, dto.Note, userId, now));
        document.UpdatedOn = now;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two uploads raced for the same version number. The upload stays, so the client can simply retry
            return null;
        }

        return versionId;
    }

    public async Task UpdateAsync(ProjectDocument document, ProjectDocumentUpdateDto dto)
    {
        if (dto.Title != null) document.Title = dto.Title.Trim();
        if (dto.TypeId != null) document.TypeId = dto.TypeId.Value;
        document.UpdatedOn = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task SetAccessAsync(ProjectDocument document, ProjectDocumentAccessUpdateDto dto)
    {
        document.Visibility = dto.Visibility;
        document.GeneralAccessType = dto.GeneralAccessType;

        // Diff instead of remove-all + re-add: EF can't track a deleted and a new row with the same key at once
        Dictionary<string, ProjectDocumentAccessType> wanted = dto.Grants.ToDictionary(g => g.UserId.ToString(), g => g.AccessType);
        foreach (ProjectDocumentAccess row in document.Access!.ToList())
        {
            if (wanted.Remove(row.UserId, out ProjectDocumentAccessType accessType))
                row.AccessType = accessType;
            else
                db.ProjectDocumentAccess.Remove(row);
        }

        db.ProjectDocumentAccess.AddRange(wanted.Select(w => new ProjectDocumentAccess
        {
            DocumentId = document.Id,
            UserId = w.Key,
            AccessType = w.Value
        }));

        document.UpdatedOn = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(ProjectDocument document, string deletedBy)
    {
        Guid[] versionIds = [.. document.Versions!.Select(v => v.Id)];

        db.ProjectDocuments.Remove(document);
        await db.SaveChangesAsync();
        logger.Information("Project document {DocumentId} ({Title}) deleted by {UserId}", document.Id, document.Title, deletedBy);

        // Best effort: whatever fails here is picked up by StorageCleanupService
        foreach (Guid versionId in versionIds)
        {
            foreach (string key in new[] { StorageKey(versionId), PreviewKey(versionId), OcrCacheKey(versionId) })
            {
                try { await storageService.DeleteObjectAsync(bucketName, key); }
                catch (Exception ex) { logger.Warning(ex, "Failed to delete {Key}, left for storage cleanup", key); }
            }
        } 
    }

    #endregion

    #region Helpers

    private static ProjectDocumentVersion NewVersion(Guid id, Guid documentId, int versionNumber, string fileName, string? note, string userId, DateTime now) => new()
    {
        Id = id,
        DocumentId = documentId,
        VersionNumber = versionNumber,
        Extension = Filetype.TrimExtension(Path.GetExtension(fileName)).ToLowerInvariant(),
        OriginalFileName = Path.GetFileNameWithoutExtension(fileName),
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        UploadedBy = userId, 
        UploadedOn = now
    };

    private static ProjectDocumentAccess NewGrant(Guid documentId, ProjectDocumentGrantDto grant) => new()
    {
        DocumentId = documentId,
        UserId = grant.UserId.ToString(),
        AccessType = grant.AccessType
    };

    private static ProjectDocumentVersion LatestVersion(ProjectDocument document)
        => document.Versions!.MaxBy(v => v.VersionNumber)!;

    private async Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string> userIds)
    {
        string[] ids = [.. userIds.Distinct()];
        return await db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");
    }

    private static ProjectDocumentVersionDto ToVersionDto(ProjectDocumentVersion version, Dictionary<string, string> names) => new()
    {
        Id = version.Id,
        VersionNumber = version.VersionNumber,
        Extension = version.Extension,
        OriginalFileName = version.OriginalFileName,
        SizeBytes = version.SizeBytes,
        Note = version.Note,
        UploadedBy = Guid.Parse(version.UploadedBy),
        UploadedByName = names.GetValueOrDefault(version.UploadedBy, "Unknown"),
        UploadedOn = version.UploadedOn,
        PreviewStatus = version.PreviewStatus,
        EmbeddingStatus = version.EmbeddingStatus
    };

    private static T ToListItem<T>(T dto, ProjectDocument document, ProjectDocumentAccessType level, Dictionary<string, string> names)
        where T : ProjectDocumentListItemDto
    {
        dto.Id = document.Id;
        dto.Title = document.Title;
        dto.TypeId = document.TypeId;
        dto.TypeName = document.Type?.Name ?? string.Empty;
        dto.Visibility = document.Visibility;
        dto.GeneralAccessType = document.GeneralAccessType;
        dto.CreatedBy = Guid.Parse(document.CreatedBy);
        dto.CreatedOn = document.CreatedOn;
        dto.UpdatedOn = document.UpdatedOn;
        dto.LatestVersion = ToVersionDto(LatestVersion(document), names);
        dto.AccessLevel = level;
        return dto;
    }

    #endregion
}