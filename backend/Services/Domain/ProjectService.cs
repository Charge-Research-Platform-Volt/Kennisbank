using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Services.Domain;

public class ProjectService(DatabaseContext db)
{
    public async Task<bool> ExistsAsync(Guid id)
        => await db.Projects.AnyAsync(p => p.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<Project, bool>> predicate)
        => await db.Projects.AnyAsync(predicate);

    public async Task<Project?> GetByIdAsync(Guid id, bool includeMembers = false, bool includeTags = false, bool includeItems = false)
    {
        IQueryable<Project> query = db.Projects.AsNoTracking();
        if (includeMembers) query = query.Include(p => p.ProjectMemberRelations);
        if (includeTags) query = query.Include(p => p.ProjectTagRelations);
        if (includeItems) query = query.Include(p => p.ProjectItemRelations);
        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ProjectListResult> GetListAsync(ProjectListRequest request)
    {
        IQueryable<Project> query = db.Projects
            .Include(p => p.ProjectTagRelations!).ThenInclude(r => r.Tag)
            .Include(p => p.ProjectMemberRelations!).ThenInclude(r => r.Member)
            .Where(p => p.ProjectType == "root")
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(p => p.Title.ToLower().Contains(request.Search.ToLower()));

        if (request.MemberFilter.HasValue)
        {
            string memberIdStr = request.MemberFilter.Value.ToString();
            query = query.Where(p => p.ProjectMemberRelations!.Any(r => r.UserId == memberIdStr));
        }

        if (request.Tags != null && request.Tags.Length > 0)
            query = query.Where(p => p.ProjectTagRelations!.Any(r => request.Tags.Contains(r.TagId)));

        if (request.StartDate.HasValue)
            query = query.Where(p => p.CreatedOn >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(p => p.CreatedOn <= request.EndDate.Value);

        int totalCount = await query.CountAsync();
        int pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);

        Project[] projects = await query
            .OrderByDescending(p => p.CreatedOn)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync();

        ProjectListItemDto[] items = projects.Select(p => new ProjectListItemDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            CreatedOn = p.CreatedOn,
            ProjectType = p.ProjectType,
            Tags = p.ProjectTagRelations?
                .Where(r => r.Tag != null)
                .Select(r => new ProjectListTagDto { Id = r.Tag!.Id, Name = r.Tag.Name })
                .ToList() ?? [],
            Members = p.ProjectMemberRelations?
                .Where(r => r.Member != null)
                .Select(r => new ProjectListMemberDto
                {
                    Id = r.Member!.Id,
                    FirstName = r.Member.FirstName,
                    LastName = r.Member.LastName,
                    CustomAvatarVersion = r.Member.HasCustom ? r.Member.CustomAvatarVersion : null,
                    HasCustom = r.Member.HasCustom
                })
                .ToList() ?? []
        }).ToArray();

        return new ProjectListResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            PageCount = pageCount
        };
    }

    public async Task<ProjectInfoDto?> GetInfoAsync(Guid id)
    {
        Project? project = await db.Projects
            .Include("ChildFolders.ChildFolder")
            .Include(p => p.ProjectTagRelations!).ThenInclude(r => r.Tag)
            .Include(p => p.ProjectMemberRelations!).ThenInclude(r => r.Member)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return null;

        var rawItems = await db.ProjectItemRelations
            .Where(r => r.ProjectId == id)
            .Join(db.LibraryItems, r => r.ItemId, g => g.Id, (r, g) => new { Relation = r, Item = g })
            .AsNoTracking()
            .ToListAsync();

        var folderUserIds = project.ChildFolders?
            .Where(r => r.AddedBy != null).Select(r => r.AddedBy!).ToHashSet() ?? [];
        var itemUserIds = rawItems
            .Where(x => x.Relation.AddedBy != null).Select(x => x.Relation.AddedBy!).ToHashSet();

        Dictionary<string, string> userNames = [];
        var allUserIds = folderUserIds.Union(itemUserIds).ToHashSet();
        if (allUserIds.Count > 0)
            userNames = await db.Users
                .Where(u => allUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        List<ProjectFolderDto> folders = project.ChildFolders?
            .Where(r => r.ChildFolder != null)
            .Select(r => new ProjectFolderDto
            {
                Id = r.ChildFolder!.Id,
                Title = r.ChildFolder.Title,
                AddedBy = r.AddedBy != null && userNames.TryGetValue(r.AddedBy, out string? n) ? n : "Unknown"
            })
            .ToList() ?? [];

        List<ProjectItem> items = rawItems.Select(x => new ProjectItem
        {
            Id = x.Item.Id,
            Name = x.Item.Name,
            Description = x.Item.Description,
            PublicationDate = x.Item.PublicationDate,
            PublicationDatePrecision = x.Item.PublicationDatePrecision,
            Type = x.Item.Type,
            FileType = x.Item.FileType,
            SourceUrl = x.Item.SourceUrl,
            CreatedOn = x.Item.CreatedOn,
            TypeId = x.Item.TypeId,
            JournalId = x.Item.JournalId,
            AddedBy = x.Relation.AddedBy != null && userNames.TryGetValue(x.Relation.AddedBy, out string? n)
                ? n : x.Relation.AddedBy ?? "Unknown"
        }).ToList();

        List<ProjectAncestor> ancestors = await GetAncestorsAsync(id);

        Project rootProject = ancestors.Count == 0
            ? project
            : await db.Projects
                .Include(p => p.ProjectTagRelations!).ThenInclude(r => r.Tag)
                .Include(p => p.ProjectMemberRelations!).ThenInclude(r => r.Member)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == ancestors[0].Id) ?? project;

        return new ProjectInfoDto
        {
            Project = project,
            RootProject = rootProject,
            Folders = folders,
            Items = items,
            Members = rootProject.ProjectMemberRelations?
                .Where(r => r.Member != null)
                .Select(r => new ProjectMemberDto
                {
                    Id = new Guid(r.Member!.Id),
                    FirstName = r.Member.FirstName,
                    LastName = r.Member.LastName,
                    Email = r.Member.Email,
                    CustomAvatarVersion = r.Member.HasCustom ? r.Member.CustomAvatarVersion : null,
                    EmailConfirmed = r.Member.EmailConfirmed
                })
                .ToList() ?? [],
            Tags = rootProject.ProjectTagRelations?.Select(r => r.Tag).ToList() ?? [],
            Ancestors = ancestors
        };
    }

    public async Task<List<ProjectAncestor>> GetAncestorsAsync(Guid id)
    {
        var ancestors = new List<ProjectAncestor>();
        Guid current = id;

        while (true)
        {
            var parent = await db.ProjectFolderRelations
                .AsNoTracking()
                .Where(r => r.ChildId == current)
                .Join(db.Projects, r => r.ParentId, p => p.Id, (r, p) => new { p.Id, p.Title })
                .FirstOrDefaultAsync();

            if (parent == null) break;
            ancestors.Add(new ProjectAncestor(parent.Id, parent.Title));
            current = parent.Id;
        }

        ancestors.Reverse();
        return ancestors;
    }

    public async Task<List<ProjectFolderDto>> GetAllFoldersAsync(Guid rootId)
    {
        var result = new List<ProjectFolderDto>();
        var stack = new Stack<(Guid Id, int Depth)>();
        stack.Push((rootId, 0));

        while (stack.Count > 0)
        {
            var (currentId, depth) = stack.Pop();
            var children = await db.ProjectFolderRelations
                .AsNoTracking()
                .Where(r => r.ParentId == currentId)
                .Join(db.Projects, r => r.ChildId, p => p.Id, (r, p) => new { p.Id, p.Title })
                .ToListAsync();

            foreach (var child in children)
            {
                result.Add(new ProjectFolderDto { Id = child.Id, Title = child.Title, Depth = depth + 1 });
                stack.Push((child.Id, depth + 1));
            }
        }

        return result;
    }

    public async Task<Guid> CreateAsync(ProjectCreateDto dto, Guid createdBy)
    {
        var id = Guid.NewGuid();
        await db.Projects.AddAsync(new Project
        {
            Id = id,
            Title = dto.Title,
            Description = dto.Description,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = createdBy,
            ProjectType = dto.ProjectType
        });

        if (dto.Tags.Length > 0)
            await db.ProjectTagRelations.AddRangeAsync(
                dto.Tags.Select(tagId => new ProjectTagRelation { ProjectId = id, TagId = tagId }));

        var memberIds = dto.Members.Append(createdBy).Distinct().Select(g => g.ToString()).ToArray();
        await db.ProjectMemberRelations.AddRangeAsync(
            memberIds.Select(userId => new ProjectMemberRelation { ProjectId = id, UserId = userId }));

        await db.SaveChangesAsync();
        return id;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Project> update)
    {
        Project? project = await db.Projects.FindAsync(id);
        if (project == null) return false;
        update(project);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task SetTagsAsync(Guid id, Guid[] tagIds)
    {
        Guid[] existing = await db.ProjectTagRelations
            .Where(r => r.ProjectId == id).Select(r => r.TagId).ToArrayAsync();

        Guid[] toRemove = existing.Except(tagIds).ToArray();
        Guid[] toAdd = tagIds.Except(existing).ToArray();

        if (toRemove.Length > 0)
            await db.ProjectTagRelations
                .Where(r => r.ProjectId == id && toRemove.Contains(r.TagId))
                .ExecuteDeleteAsync();

        if (toAdd.Length > 0)
        {
            await db.ProjectTagRelations.AddRangeAsync(
                toAdd.Select(tagId => new ProjectTagRelation { ProjectId = id, TagId = tagId }));
            await db.SaveChangesAsync();
        }
    }

    public async Task SetMembersAsync(Guid id, Guid[] memberIds)
    {
        string[] existing = await db.ProjectMemberRelations
            .Where(r => r.ProjectId == id).Select(r => r.UserId).ToArrayAsync();

        string[] newIds = memberIds.Select(g => g.ToString()).ToArray();
        string[] toRemove = existing.Except(newIds).ToArray();
        string[] toAdd = newIds.Except(existing).ToArray();

        if (toRemove.Length > 0)
            await CascadeMemberOperationAsync(id, async projectId =>
                await db.ProjectMemberRelations
                    .Where(r => r.ProjectId == projectId && toRemove.Contains(r.UserId))
                    .ExecuteDeleteAsync());

        if (toAdd.Length > 0)
            await CascadeMemberOperationAsync(id, async projectId =>
            {
                string[] present = await db.ProjectMemberRelations
                    .Where(r => r.ProjectId == projectId).Select(r => r.UserId).ToArrayAsync();
                string[] missing = toAdd.Except(present).ToArray();
                if (missing.Length > 0)
                {
                    await db.ProjectMemberRelations.AddRangeAsync(
                        missing.Select(userId => new ProjectMemberRelation { ProjectId = projectId, UserId = userId }));
                    await db.SaveChangesAsync();
                }
            });
    }

    public async Task<Guid> AddFolderAsync(Guid parentId, string name, Guid createdBy)
    {
        string[] parentMembers = await db.ProjectMemberRelations
            .Where(r => r.ProjectId == parentId).Select(r => r.UserId).ToArrayAsync();

        string createdByStr = createdBy.ToString();
        string[] members = parentMembers.Append(createdByStr).Distinct().ToArray();

        Guid folderId = Guid.NewGuid();
        await db.Projects.AddAsync(new Project
        {
            Id = folderId,
            Title = name,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = createdBy,
            ProjectType = "folder"
        });
        await db.ProjectMemberRelations.AddRangeAsync(
            members.Select(userId => new ProjectMemberRelation { ProjectId = folderId, UserId = userId }));
        await db.ProjectFolderRelations.AddAsync(new ProjectFolderRelation
        {
            ParentId = parentId,
            ChildId = folderId,
            AddedBy = createdByStr
        });

        await db.SaveChangesAsync();
        return folderId;
    }

    public async Task AddItemAsync(Guid projectId, Guid itemId, Guid addedBy)
    {
        await db.ProjectItemRelations.AddAsync(new ProjectItemRelation
        {
            ProjectId = projectId,
            ItemId = itemId,
            AddedBy = addedBy.ToString()
        });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveItemAsync(Guid projectId, Guid itemId)
    {
        int deleted = await db.ProjectItemRelations
            .Where(r => r.ProjectId == projectId && r.ItemId == itemId)
            .ExecuteDeleteAsync();
        return deleted > 0;
    }

    public async Task<bool> ItemAlreadyLinkedAsync(Guid projectId, Guid itemId)
        => await db.ProjectItemRelations.AnyAsync(r => r.ProjectId == projectId && r.ItemId == itemId);

    public async Task<Guid[]> GetProjectItemIdsAsync(Guid projectId)
        => await db.ProjectItemRelations
            .Where(r => r.ProjectId == projectId)
            .Select(r => r.ItemId)
            .ToArrayAsync();

    public async Task<bool> DeleteAsync(Guid id)
    {
        await DeleteProjectTreeAsync(id);
        return true;
    }

    private async Task DeleteProjectTreeAsync(Guid projectId)
    {
        var childIds = await db.ProjectFolderRelations
            .Where(r => r.ParentId == projectId).Select(r => r.ChildId).ToListAsync();

        foreach (var childId in childIds)
            await DeleteProjectTreeAsync(childId);

        await db.ProjectItemRelations.Where(r => r.ProjectId == projectId).ExecuteDeleteAsync();
        await db.ProjectMemberRelations.Where(r => r.ProjectId == projectId).ExecuteDeleteAsync();
        await db.ProjectTagRelations.Where(r => r.ProjectId == projectId).ExecuteDeleteAsync();
        await db.ProjectFolderRelations.Where(r => r.ChildId == projectId).ExecuteDeleteAsync();
        await db.Projects.Where(p => p.Id == projectId).ExecuteDeleteAsync();
    }

    private async Task CascadeMemberOperationAsync(Guid projectId, Func<Guid, Task> operation)
    {
        var queue = new Queue<Guid>();
        queue.Enqueue(projectId);

        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            await operation(current);

            var childIds = await db.ProjectFolderRelations
                .Where(r => r.ParentId == current).Select(r => r.ChildId).ToListAsync();

            foreach (var childId in childIds)
                queue.Enqueue(childId);
        }
    }
}