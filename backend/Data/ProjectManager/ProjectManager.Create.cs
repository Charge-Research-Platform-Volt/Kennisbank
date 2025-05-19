using KnowledgeBank.Models;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        public async Task<Guid> CreateProject(ProjectCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();

            // Generate new ID for the project
            Guid projectId = Guid.NewGuid();

            // Create a project instance using the DTO
            Project project = new()
            {
                Id = projectId,
                Title = dto.Title,
                Description = dto.Description,
                CreationDate = dto.CreationDate,
                DeletionDate = dto.DeletionDate,
                ProjectType = dto.ProjectType
            };

            if (dto.Creators != null)
            {
                await AddCreatorToProjectRangeAsync(projectId, dto.Creators);
            }

            if (dto.Tags != null)
            {
                await AddTagToProjectRangeAsync(projectId, dto.Tags);
            }

            // Add project to database
            await database.Projects.AddAsync(project);

            if (startedTransaction) await Commit();

            return projectId;
        }
    }
}