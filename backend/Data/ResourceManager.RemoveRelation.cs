using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend.Data
{
    // This part is for removing relations
    public partial class ResourceManager
    {
        #region Organisation Relationships

        // Remove single

        public async Task<bool> RemoveOrganisationRelationshipAsync(Guid sourceOrganisationId, Guid targetOrganisationId)
        { return await DeleteAsync(database.OrganisationRelationships, relationship => relationship.SourceOrganisationId == sourceOrganisationId && relationship.TargetOrganisationId == targetOrganisationId) > 0; }

        public async Task<bool> RemoveOrganisationRelationshipAsync(Guid sourceOrganisationId, string targetOrganisationId)
        { return await RemoveOrganisationRelationshipAsync(sourceOrganisationId, Guid.Parse(targetOrganisationId)); }

        public async Task<bool> RemoveOrganisationRelationshipAsync(string sourceOrganisationId, Guid targetOrganisationId)
        { return await RemoveOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), targetOrganisationId); }

        public async Task<bool> RemoveOrganisationRelationshipAsync(string sourceOrganisationId, string targetOrganisationId)
        { return await RemoveOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), Guid.Parse(targetOrganisationId)); }

        // Prune

        public async Task<bool> RemoveAllOrganisationRelationshipsWithSourceIdAsync(Guid sourceOrganisationId)
        { return await DeleteAllWhereAsync(database.OrganisationRelationships, i => i.SourceOrganisationId == sourceOrganisationId) > 0; }

        public async Task<bool> RemoveAllOrganisationRelationshipsWithSourceIdAsync(string sourceOrganisationId)
        { return await RemoveAllOrganisationRelationshipsWithSourceIdAsync(Guid.Parse(sourceOrganisationId)); }



        public async Task<bool> RemoveAllOrganisationRelationshipsWithTargetIdAsync(Guid targetOrganisationId)
        { return await DeleteAllWhereAsync(database.OrganisationRelationships, i => i.TargetOrganisationId == targetOrganisationId) > 0; }

        public async Task<bool> RemoveAllOrganisationRelationshipsWithTargetIdAsync(string targetOrganisationId)
        { return await RemoveAllOrganisationRelationshipsWithTargetIdAsync(Guid.Parse(targetOrganisationId)); }



        public async Task<bool> RemoveAllOrganisationRelationshipsContainingIdAsync(Guid organisationId)
        { return await DeleteAllWhereAsync(database.OrganisationRelationships, i => i.SourceOrganisationId == organisationId || i.TargetOrganisationId == organisationId) > 0; }

        public async Task<bool> RemoveAllOrganisationRelationshipsContainingIdAsync(string organisationId)
        { return await RemoveAllOrganisationRelationshipsContainingIdAsync(Guid.Parse(organisationId)); }

        #endregion

        #region Person-Organisation

        // Remove single

        public async Task<bool> RemovePersonFromOrganisationAsync(Guid personId, Guid organisationId)
        { return await DeleteAsync(database.PersonOrganisationRelations, relation => relation.PersonId == personId && relation.OrganisationId == organisationId) > 0; }

        public async Task<bool> RemovePersonFromOrganisationAsync(Guid personId, string organisationId)
        { return await RemovePersonFromOrganisationAsync(personId, Guid.Parse(organisationId)); }

        public async Task<bool> RemovePersonFromOrganisationAsync(string personId, Guid organisationId)
        { return await RemovePersonFromOrganisationAsync(Guid.Parse(personId), organisationId); }

        public async Task<bool> RemovePersonFromOrganisationAsync(string personId, string organisationId)
        { return await RemovePersonFromOrganisationAsync(Guid.Parse(personId), Guid.Parse(organisationId)); }

        // Prune

        public async Task<bool> RemovePersonFromAllOrganisationsAsync(Guid personId)
        { return await DeleteAllWhereAsync(database.PersonOrganisationRelations, i => i.PersonId == personId) > 0; }

        public async Task<bool> RemovePersonFromAllOrganisationsAsync(string personId)
        { return await RemovePersonFromAllOrganisationsAsync(Guid.Parse(personId)); }



        public async Task<bool> RemoveAllPersonOrganisationRelationsWithOrganisationIdAsync(Guid organisationId)
        { return await DeleteAllWhereAsync(database.PersonOrganisationRelations, i => i.OrganisationId == organisationId) > 0; }

        public async Task<bool> RemoveAllPersonOrganisationRealtionsWithOrganisationIdAsync(string organisationId)
        { return await RemoveAllPersonOrganisationRelationsWithOrganisationIdAsync(Guid.Parse(organisationId)); }

        #endregion

        #region Person Relationships

        // Remove single

        public async Task<bool> RemovePersonRelationshipAsync(Guid sourcePersonId, Guid targetPersonId)
        { return await DeleteAsync(database.PersonRelationships, relation => relation.SourcePersonId == sourcePersonId && relation.TargetPersonId == targetPersonId) > 0; }

        public async Task<bool> RemovePersonRelationshipAsync(Guid sourcePersonId, string targetPersonId)
        { return await RemovePersonRelationshipAsync(sourcePersonId, Guid.Parse(targetPersonId)); }

        public async Task<bool> RemovePersonRelationshipAsync(string sourcePersonId, Guid targetPersonId)
        { return await RemovePersonRelationshipAsync(Guid.Parse(sourcePersonId), targetPersonId); }

        public async Task<bool> RemovePersonRelationshipAsync(string sourcePersonId, string targetPersonId)
        { return await RemovePersonRelationshipAsync(Guid.Parse(sourcePersonId), Guid.Parse(targetPersonId)); }

        // Prune

        public async Task<bool> RemoveAllPersonRelationshipsWithSourceIdAsync(Guid sourcePersonId)
        { return await DeleteAllWhereAsync(database.PersonRelationships, i => i.SourcePersonId == sourcePersonId) > 0   ; }

        public async Task<bool> RemoveAllPersonRelationshipsWithSourceIdAsync(string sourcePersonId)
        { return await RemoveAllPersonRelationshipsWithSourceIdAsync(Guid.Parse(sourcePersonId)); }



        public async Task<bool> RemoveAllPersonRelationshipsWithTargetIdAsync(Guid targetPersonId)
        { return await DeleteAllWhereAsync(database.PersonRelationships, i => i.TargetPersonId == targetPersonId) > 0; }

        public async Task<bool> RemoveAllPersonRelationshipsWithTargetIdAsync(string targetPersonId)
        { return await RemoveAllPersonRelationshipsWithTargetIdAsync(Guid.Parse(targetPersonId)); }



        public async Task<bool> RemoveAllPersonRelationshipsContainingIdAsync(Guid personId)
        { return await DeleteAllWhereAsync(database.PersonRelationships, i => i.SourcePersonId == personId || i.TargetPersonId == personId) > 0; }

        public async Task<bool> RemoveAllPersonRelationshipsContainingIdAsync(string personId)
        { return await RemoveAllPersonRelationshipsContainingIdAsync(Guid.Parse(personId)); }

        #endregion

        #region Resource-Person (Author)

        // Remove single

        public async Task<bool> RemoveAuthorFromResourceAsync(Guid resourceId, Guid personId)
        { return await DeleteAsync(database.ResourceAuthorRelations, relation => relation.ResourceId == resourceId && relation.PersonId == personId) > 0; }

        public async Task<bool> RemoveAuthorFromResourceAsync(string resourceId, Guid personId)
        { return await RemoveAuthorFromResourceAsync(Guid.Parse(resourceId), personId); }

        public async Task<bool> RemoveAuthorFromResourceAsync(Guid resourceId, string personId)
        { return await RemoveAuthorFromResourceAsync(resourceId, Guid.Parse(personId)); }

        public async Task<bool> RemoveAuthorFromResourceAsync(string resourceId, string personId)
        { return await RemoveAuthorFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(personId)); }

        // Prune

        public async Task<bool> RemoveAuthorFromAllResourcesAsync(Guid personId)
        { return await DeleteAllWhereAsync(database.ResourceAuthorRelations, i => i.PersonId == personId) > 0; }

        public async Task<bool> RemoveAuthorFromAllResourcesAsync(string personId)
        { return await RemoveAuthorFromAllResourcesAsync(Guid.Parse(personId)); }



        public async Task<bool> RemoveAllResourceAuthorRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceAuthorRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceAuthorRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceAuthorRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Organisation (Direct)

        // Remove single

        public async Task<bool> RemoveOrganisationFromResourceAsync(Guid resourceId, Guid organisationId)
        { return await DeleteAsync(database.ResourceOrganisationRelations, relation => relation.ResourceId == resourceId && relation.OrganisationId == organisationId) > 0; }

        public async Task<bool> RemoveOrganisationFromResourceAsync(string resourceId, Guid organisationId)
        { return await RemoveOrganisationFromResourceAsync(Guid.Parse(resourceId), organisationId); }

        public async Task<bool> RemoveOrganisationFromResourceAsync(Guid resourceId, string organisationId)
        { return await RemoveOrganisationFromResourceAsync(resourceId, Guid.Parse(organisationId)); }

        public async Task<bool> RemoveOrganisationFromResourceAsync(string resourceId, string organisationId)
        { return await RemoveOrganisationFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(organisationId)); }

        // Prune

        public async Task<bool> RemoveOrganisationFromAllResourcesAsync(Guid organisationId)
        { return await DeleteAllWhereAsync(database.ResourceOrganisationRelations, i => i.OrganisationId == organisationId) > 0; }

        public async Task<bool> RemoveOrganisationFromAllResourcesAsync(string organisationId)
        { return await RemoveOrganisationFromAllResourcesAsync(Guid.Parse(organisationId)); }



        public async Task<bool> RemoveAllResourceOrganisationRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceOrganisationRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceOrganisationRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceOrganisationRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Region

        // Remove single

        public async Task<bool> RemoveRegionFromResourceAsync(Guid resourceId, Guid regionId)
        { return await DeleteAsync(database.ResourceRegionRelations, relation => relation.ResourceId == resourceId && relation.RegionId == regionId) > 0; }

        public async Task<bool> RemoveRegionFromResourceAsync(string resourceId, Guid regionId)
        { return await RemoveRegionFromResourceAsync(Guid.Parse(resourceId), regionId); }

        public async Task<bool> RemoveRegionFromResourceAsync(Guid resourceId, string regionId)
        { return await RemoveRegionFromResourceAsync(resourceId, Guid.Parse(regionId)); }

        public async Task<bool> RemoveRegionFromResourceAsync(string resourceId, string regionId)
        { return await RemoveRegionFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(regionId)); }

        // Prune

        public async Task<bool> RemoveRegionFromAllResourcesAsync(Guid regionId)
        { return await DeleteAllWhereAsync(database.ResourceRegionRelations, i => i.RegionId == regionId) > 0; }

        public async Task<bool> RemoveRegionFromAllResourcesAsync(string regionId)
        { return await RemoveRegionFromAllResourcesAsync(Guid.Parse(regionId)); }



        public async Task<bool> RemoveAllResourceRegionRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceRegionRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceRegionRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceRegionRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Source

        public async Task<bool> RemoveSourceFromResourceAsync(Guid resourceId, string url)
        { return await DeleteAsync(database.ResourceSourceRelations, relation => relation.ResourceId == resourceId && relation.Url == url) > 0; }

        public async Task<bool> RemoveSourceFromResourceAsync(string resourceId, string url)
        { return await RemoveSourceFromResourceAsync(Guid.Parse(resourceId), url); }



        public async Task<bool> RemoveAllResourceSourceRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceSourceRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceSourceRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceSourceRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Related Organisation (Indirect)

        // Remove single

        public async Task<bool> RemoveRelatedOrganisationFromResourceAsync(Guid resourceId, Guid organisationId)
        { return await DeleteAsync(database.ResourceRelatedOrganisationRelations, relation => relation.ResourceId == resourceId && relation.OrganisationId == organisationId) > 0; }

        public async Task<bool> RemoveRelatedOrganisationFromResourceAsync(string resourceId, Guid organisationId)
        { return await RemoveRelatedOrganisationFromResourceAsync(Guid.Parse(resourceId), organisationId); }

        public async Task<bool> RemoveRelatedOrganisationFromResourceAsync(Guid resourceId, string organisationId)
        { return await RemoveRelatedOrganisationFromResourceAsync(resourceId, Guid.Parse(organisationId)); }

        public async Task<bool> RemoveRelatedOrganisationFromResourceAsync(string resourceId, string organisationId)
        { return await RemoveRelatedOrganisationFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(organisationId)); }

        // Prune

        public async Task<bool> RemoveRelatedOrganisationFromAllResourcesAsync(Guid organisationId)
        { return await DeleteAllWhereAsync(database.ResourceRelatedOrganisationRelations, i => i.OrganisationId == organisationId) > 0; }

        public async Task<bool> RemoveRelatedOrganisationFromAllResourcesAsync(string organisationId)
        { return await RemoveRelatedOrganisationFromAllResourcesAsync(Guid.Parse(organisationId)); }



        public async Task<bool> RemoveAllResourceRelatedOrganisationRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceRelatedOrganisationRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceRelatedOrganisationRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceRelatedOrganisationRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Related Person (non-author)

        // Remove single

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(Guid resourceId, Guid personId)
        { return await DeleteAsync(database.ResourceRelatedPersonRelations, relation => relation.ResourceId == resourceId && relation.PersonId == personId) > 0; }

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(string resourceId, Guid personId)
        { return await RemoveRelatedPersonFromResourceAsync(Guid.Parse(resourceId), personId); }

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(Guid resourceId, string personId)
        { return await RemoveRelatedPersonFromResourceAsync(resourceId, Guid.Parse(personId)); }

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(string resourceId, string personId)
        { return await RemoveRelatedPersonFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(personId)); }

        // Prune

        public async Task<bool> RemoveRelatedPersonFromAllResourcesAsync(Guid personId)
        { return await DeleteAllWhereAsync(database.ResourceRelatedPersonRelations, i => i.PersonId == personId) > 0; }

        public async Task<bool> RemoveRelatedPersonFromAllResourcesAsync(string personId)
        { return await RemoveRelatedPersonFromAllResourcesAsync(Guid.Parse(personId)); }



        public async Task<bool> RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceRelatedPersonRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Related_source

        public async Task<bool> RemoveRelatedSourceFromResourceAsync(Guid resourceId, string url)
        { return await DeleteAsync(database.ResourceRelatedSourceRelations, relation => relation.ResourceId == resourceId && relation.Url == url) > 0; }

        public async Task<bool> RemoveRelatedSourceFromResourceAsync(string resourceId, string url)
        { return await RemoveRelatedSourceFromResourceAsync(Guid.Parse(resourceId), url); }



        public async Task<bool> RemoveAllResourceRelatedSourceRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceRelatedSourceRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceRelatedSourceRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceRelatedSourceRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-AdminTag

        // Remove single

        public async Task<bool> RemoveAdminTagFromResourceAsync(Guid resourceId, Guid tagId)
        { return await DeleteAsync(database.ResourceAdminTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId) > 0; }

        public async Task<bool> RemoveAdminTagFromResourceAsync(string resourceId, Guid tagId)
        { return await RemoveAdminTagFromResourceAsync(Guid.Parse(resourceId), tagId); }

        public async Task<bool> RemoveAdminTagFromResourceAsync(Guid resourceId, string tagId)
        { return await RemoveAdminTagFromResourceAsync(resourceId, Guid.Parse(tagId)); }

        public async Task<bool> RemoveAdminTagFromResourceAsync(string resourceId, string tagId)
        { return await RemoveAdminTagFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(tagId)); }

        // Prune

        public async Task<bool> RemoveAdminTagFromAllResourcesAsync(Guid tagId)
        { return await DeleteAllWhereAsync(database.ResourceAdminTagRelations, i => i.TagId == tagId) > 0; }

        public async Task<bool> RemoveAdminTagFromAllResourcesAsync(string tagId)
        { return await RemoveAdminTagFromAllResourcesAsync(Guid.Parse(tagId)); }



        public async Task<bool> RemoveAllResourceAdminTagRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceAdminTagRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceAdminTagRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceAdminTagRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-UserTag

        // Remove single

        public async Task<bool> RemoveUserTagFromResourceAsync(Guid resourceId, Guid tagId)
        { return await DeleteAsync(database.ResourceUserTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId) > 0; }

        public async Task<bool> RemoveUserTagFromResourceAsync(string resourceId, Guid tagId)
        { return await RemoveUserTagFromResourceAsync(Guid.Parse(resourceId), tagId); }

        public async Task<bool> RemoveUserTagFromResourceAsync(Guid resourceId, string tagId)
        { return await RemoveUserTagFromResourceAsync(resourceId, Guid.Parse(tagId)); }

        public async Task<bool> RemoveUserTagFromResourceAsync(string resourceId, string tagId)
        { return await RemoveUserTagFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(tagId)); }

        // Prune

        public async Task<bool> RemoveUserTagFromAllResourcesAsync(Guid tagId)
        { return await DeleteAllWhereAsync(database.ResourceUserTagRelations, i => i.TagId == tagId) > 0; }

        public async Task<bool> RemoveUserTagFromAllResourcesAsync(string tagId)
        { return await RemoveUserTagFromAllResourcesAsync(Guid.Parse(tagId)); }



        public async Task<bool> RemoveAllResourceUserTagRelationsWithResourceIdAsync(Guid resourceId)
        { return await DeleteAllWhereAsync(database.ResourceUserTagRelations, i => i.ResourceId == resourceId) > 0; }

        public async Task<bool> RemoveAllResourceUserTagRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceUserTagRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-ResourceType

        public async Task<bool> RemoveResourceTypeFromResourceAsync(Guid resourceId)
        {
            await AddResourceTypeToResourceAsync(resourceId, DatabaseSeeder.UnknownResourceTypeId);
            return true;
        }

        #endregion
    }
}
