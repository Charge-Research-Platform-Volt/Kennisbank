// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

namespace KnowledgeBank.Data
{
    // This part is for removing relations
    public partial class ResourceManager
    {
        #region Organisation Relationships

        // Remove single

        public async Task<bool> RemoveOrganisationRelationshipAsync(Guid sourceOrganisationId, Guid targetOrganisationId)
        {
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.OrganisationRelationships,
            relationship => relationship.SourceOrganisationId == sourceOrganisationId && relationship.TargetOrganisationId == targetOrganisationId
            || relationship.SourceOrganisationId == targetOrganisationId && relationship.TargetOrganisationId == sourceOrganisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveOrganisationRelationshipAsync(Guid sourceOrganisationId, string targetOrganisationId)
        { return await RemoveOrganisationRelationshipAsync(sourceOrganisationId, Guid.Parse(targetOrganisationId)); }

        public async Task<bool> RemoveOrganisationRelationshipAsync(string sourceOrganisationId, Guid targetOrganisationId)
        { return await RemoveOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), targetOrganisationId); }

        public async Task<bool> RemoveOrganisationRelationshipAsync(string sourceOrganisationId, string targetOrganisationId)
        { return await RemoveOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), Guid.Parse(targetOrganisationId)); }

        // Prune

        public async Task<bool> RemoveAllOrganisationRelationshipsWithSourceIdAsync(Guid sourceOrganisationId)
        {
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.OrganisationRelationships, i => i.SourceOrganisationId == sourceOrganisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllOrganisationRelationshipsWithSourceIdAsync(string sourceOrganisationId)
        { return await RemoveAllOrganisationRelationshipsWithSourceIdAsync(Guid.Parse(sourceOrganisationId)); }



        public async Task<bool> RemoveAllOrganisationRelationshipsWithTargetIdAsync(Guid targetOrganisationId)
        {
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.OrganisationRelationships, i => i.TargetOrganisationId == targetOrganisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllOrganisationRelationshipsWithTargetIdAsync(string targetOrganisationId)
        { return await RemoveAllOrganisationRelationshipsWithTargetIdAsync(Guid.Parse(targetOrganisationId)); }



        public async Task<bool> RemoveAllOrganisationRelationshipsContainingIdAsync(Guid organisationId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.OrganisationRelationships, i => i.SourceOrganisationId == organisationId || i.TargetOrganisationId == organisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllOrganisationRelationshipsContainingIdAsync(string organisationId)
        { return await RemoveAllOrganisationRelationshipsContainingIdAsync(Guid.Parse(organisationId)); }

        #endregion

        #region Person-Organisation

        // Remove single

        public async Task<bool> RemovePersonFromOrganisationAsync(Guid personId, Guid organisationId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.PersonOrganisationRelations, relation => relation.PersonId == personId && relation.OrganisationId == organisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemovePersonFromOrganisationAsync(Guid personId, string organisationId)
        { return await RemovePersonFromOrganisationAsync(personId, Guid.Parse(organisationId)); }

        public async Task<bool> RemovePersonFromOrganisationAsync(string personId, Guid organisationId)
        { return await RemovePersonFromOrganisationAsync(Guid.Parse(personId), organisationId); }

        public async Task<bool> RemovePersonFromOrganisationAsync(string personId, string organisationId)
        { return await RemovePersonFromOrganisationAsync(Guid.Parse(personId), Guid.Parse(organisationId)); }

        // Prune

        public async Task<bool> RemovePersonFromAllOrganisationsAsync(Guid personId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.PersonOrganisationRelations, i => i.PersonId == personId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemovePersonFromAllOrganisationsAsync(string personId)
        { return await RemovePersonFromAllOrganisationsAsync(Guid.Parse(personId)); }



        public async Task<bool> RemoveAllPersonOrganisationRelationsWithOrganisationIdAsync(Guid organisationId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.PersonOrganisationRelations, i => i.OrganisationId == organisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllPersonOrganisationRealtionsWithOrganisationIdAsync(string organisationId)
        { return await RemoveAllPersonOrganisationRelationsWithOrganisationIdAsync(Guid.Parse(organisationId)); }

        #endregion

        #region Person Relationships

        // Remove single

        public async Task<bool> RemovePersonRelationshipAsync(Guid sourcePersonId, Guid targetPersonId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.PersonRelationships, relation => relation.SourcePersonId == sourcePersonId && relation.TargetPersonId == targetPersonId
            || relation.SourcePersonId == targetPersonId && relation.TargetPersonId == sourcePersonId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemovePersonRelationshipAsync(Guid sourcePersonId, string targetPersonId)
        { return await RemovePersonRelationshipAsync(sourcePersonId, Guid.Parse(targetPersonId)); }

        public async Task<bool> RemovePersonRelationshipAsync(string sourcePersonId, Guid targetPersonId)
        { return await RemovePersonRelationshipAsync(Guid.Parse(sourcePersonId), targetPersonId); }

        public async Task<bool> RemovePersonRelationshipAsync(string sourcePersonId, string targetPersonId)
        { return await RemovePersonRelationshipAsync(Guid.Parse(sourcePersonId), Guid.Parse(targetPersonId)); }

        // Prune

        public async Task<bool> RemoveAllPersonRelationshipsWithSourceIdAsync(Guid sourcePersonId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.PersonRelationships, i => i.SourcePersonId == sourcePersonId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllPersonRelationshipsWithSourceIdAsync(string sourcePersonId)
        { return await RemoveAllPersonRelationshipsWithSourceIdAsync(Guid.Parse(sourcePersonId)); }



        public async Task<bool> RemoveAllPersonRelationshipsWithTargetIdAsync(Guid targetPersonId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.PersonRelationships, i => i.TargetPersonId == targetPersonId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllPersonRelationshipsWithTargetIdAsync(string targetPersonId)
        { return await RemoveAllPersonRelationshipsWithTargetIdAsync(Guid.Parse(targetPersonId)); }



        public async Task<bool> RemoveAllPersonRelationshipsContainingIdAsync(Guid personId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.PersonRelationships, i => i.SourcePersonId == personId || i.TargetPersonId == personId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllPersonRelationshipsContainingIdAsync(string personId)
        { return await RemoveAllPersonRelationshipsContainingIdAsync(Guid.Parse(personId)); }

        #endregion

        #region Resource-Person (Author)

        // Remove single

        public async Task<bool> RemoveAuthorFromResourceAsync(Guid resourceId, Guid authorId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.ResourceAuthorRelations, relation => relation.ResourceId == resourceId && relation.AuthorId == authorId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAuthorFromResourceAsync(string resourceId, Guid personId)
        { return await RemoveAuthorFromResourceAsync(Guid.Parse(resourceId), personId); }

        public async Task<bool> RemoveAuthorFromResourceAsync(Guid resourceId, string personId)
        { return await RemoveAuthorFromResourceAsync(resourceId, Guid.Parse(personId)); }

        public async Task<bool> RemoveAuthorFromResourceAsync(string resourceId, string personId)
        { return await RemoveAuthorFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(personId)); }

        // Prune

        public async Task<bool> RemoveAuthorFromAllResourcesAsync(Guid authorId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceAuthorRelations, i => i.AuthorId == authorId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAuthorFromAllResourcesAsync(string authorId)
        { return await RemoveAuthorFromAllResourcesAsync(Guid.Parse(authorId)); }



        public async Task<bool> RemoveAllResourceAuthorRelationsWithResourceIdAsync(Guid resourceId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceAuthorRelations, i => i.ResourceId == resourceId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllResourceAuthorRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceAuthorRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Organisation (Direct)

        // Remove single

        public async Task<bool> RemoveOrganisationFromResourceAsync(Guid resourceId, Guid organisationId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.ResourceOrganisationRelations, relation => relation.ResourceId == resourceId && relation.OrganisationId == organisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveOrganisationFromResourceAsync(string resourceId, Guid organisationId)
        { return await RemoveOrganisationFromResourceAsync(Guid.Parse(resourceId), organisationId); }

        public async Task<bool> RemoveOrganisationFromResourceAsync(Guid resourceId, string organisationId)
        { return await RemoveOrganisationFromResourceAsync(resourceId, Guid.Parse(organisationId)); }

        public async Task<bool> RemoveOrganisationFromResourceAsync(string resourceId, string organisationId)
        { return await RemoveOrganisationFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(organisationId)); }

        // Prune

        public async Task<bool> RemoveOrganisationFromAllResourcesAsync(Guid organisationId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceOrganisationRelations, i => i.OrganisationId == organisationId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveOrganisationFromAllResourcesAsync(string organisationId)
        { return await RemoveOrganisationFromAllResourcesAsync(Guid.Parse(organisationId)); }



        public async Task<bool> RemoveAllResourceOrganisationRelationsWithResourceIdAsync(Guid resourceId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceOrganisationRelations, i => i.ResourceId == resourceId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllResourceOrganisationRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceOrganisationRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Region

        // Remove single

        public async Task<bool> RemoveRegionFromResourceAsync(Guid resourceId, Guid regionId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.ResourceRegionRelations, relation => relation.ResourceId == resourceId && relation.RegionId == regionId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveRegionFromResourceAsync(string resourceId, Guid regionId)
        { return await RemoveRegionFromResourceAsync(Guid.Parse(resourceId), regionId); }

        public async Task<bool> RemoveRegionFromResourceAsync(Guid resourceId, string regionId)
        { return await RemoveRegionFromResourceAsync(resourceId, Guid.Parse(regionId)); }

        public async Task<bool> RemoveRegionFromResourceAsync(string resourceId, string regionId)
        { return await RemoveRegionFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(regionId)); }

        // Prune

        public async Task<bool> RemoveRegionFromAllResourcesAsync(Guid regionId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceRegionRelations, i => i.RegionId == regionId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveRegionFromAllResourcesAsync(string regionId)
        { return await RemoveRegionFromAllResourcesAsync(Guid.Parse(regionId)); }



        public async Task<bool> RemoveAllResourceRegionRelationsWithResourceIdAsync(Guid resourceId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceRegionRelations, i => i.ResourceId == resourceId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllResourceRegionRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceRegionRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Source

        public async Task<bool> RemoveSourceFromResourceAsync(Guid resourceId, string url)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.ResourceSourceRelations, relation => relation.ResourceId == resourceId && relation.Url == url) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveSourceFromResourceAsync(string resourceId, string url)
        { return await RemoveSourceFromResourceAsync(Guid.Parse(resourceId), url); }



        public async Task<bool> RemoveAllResourceSourceRelationsWithResourceIdAsync(Guid resourceId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceSourceRelations, i => i.ResourceId == resourceId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllResourceSourceRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceSourceRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Related Person (non-author)

        // Remove single

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(Guid resourceId, Guid personId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.ResourceRelatedPersonRelations, relation => relation.ResourceId == resourceId && relation.PersonId == personId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(string resourceId, Guid personId)
        { return await RemoveRelatedPersonFromResourceAsync(Guid.Parse(resourceId), personId); }

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(Guid resourceId, string personId)
        { return await RemoveRelatedPersonFromResourceAsync(resourceId, Guid.Parse(personId)); }

        public async Task<bool> RemoveRelatedPersonFromResourceAsync(string resourceId, string personId)
        { return await RemoveRelatedPersonFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(personId)); }

        // Prune

        public async Task<bool> RemoveRelatedPersonFromAllResourcesAsync(Guid personId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceRelatedPersonRelations, i => i.PersonId == personId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveRelatedPersonFromAllResourcesAsync(string personId)
        { return await RemoveRelatedPersonFromAllResourcesAsync(Guid.Parse(personId)); }



        public async Task<bool> RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(Guid resourceId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceRelatedPersonRelations, i => i.ResourceId == resourceId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-Tag

        // Remove single

        public async Task<bool> RemoveTagFromResourceAsync(Guid resourceId, Guid tagId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAsync(database.ResourceTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId) > 0; 
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveTagFromResourceAsync(string resourceId, Guid tagId)
        { return await RemoveTagFromResourceAsync(Guid.Parse(resourceId), tagId); }

        public async Task<bool> RemoveTagFromResourceAsync(Guid resourceId, string tagId)
        { return await RemoveTagFromResourceAsync(resourceId, Guid.Parse(tagId)); }

        public async Task<bool> RemoveTagFromResourceAsync(string resourceId, string tagId)
        { return await RemoveTagFromResourceAsync(Guid.Parse(resourceId), Guid.Parse(tagId)); }

        // Prune

        public async Task<bool> RemoveTagFromAllResourcesAsync(Guid tagId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceTagRelations, i => i.TagId == tagId) > 0;
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveTagFromAllResourcesAsync(string tagId)
        { return await RemoveTagFromAllResourcesAsync(Guid.Parse(tagId)); }



        public async Task<bool> RemoveAllResourceTagRelationsWithResourceIdAsync(Guid resourceId)
        { 
            bool startedTransaction = await BeginTransaction();
            bool deleted = await DeleteAllWhereAsync(database.ResourceTagRelations, i => i.ResourceId == resourceId) > 0;
            if (startedTransaction) await Commit();
            return deleted;
        }

        public async Task<bool> RemoveAllResourceTagRelationsWithResourceIdAsync(string resourceId)
        { return await RemoveAllResourceTagRelationsWithResourceIdAsync(Guid.Parse(resourceId)); }

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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


