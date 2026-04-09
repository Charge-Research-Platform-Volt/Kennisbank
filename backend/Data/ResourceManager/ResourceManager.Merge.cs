using KnowledgeBank.Models;

namespace KnowledgeBank.Data;

public partial class ResourceManager
{
    // --- Person

    public async Task<bool> MergePersonsAsync(Guid survivorId, Guid sourceId)
    {
        bool startedTransaction = await BeginTransaction();

        // Transfer authored resource relations
        ResourceAuthorRelation[] authoredResources = await GetAllResourceAuthorRelationsAsync(predicate: r => r.AuthorId == sourceId);

        foreach (ResourceAuthorRelation relation in authoredResources)
        {
            bool alreadyExists = await ResourceAuthorRelationExistsAsync(r => r.ResourceId == relation.ResourceId && r.AuthorId == survivorId);

            if (!alreadyExists)
                await AddAuthorToResourceAsync(relation.ResourceId, survivorId);
        }

        // Transfer related resource relations
        ResourceRelatedPersonRelation[] relatedResources = await GetAllResourceRelatedPersonRelationsAsync(predicate: r => r.PersonId == sourceId);

        foreach (ResourceRelatedPersonRelation relation in relatedResources)
        {
            bool alreadyExists = await ResourceRelatedPersonRelationsExistsAsync(r => r.ResourceId == relation.ResourceId && r.PersonId == survivorId);

            if (!alreadyExists)
                await AddRelatedPersonToResourceAsync(relation.ResourceId, survivorId);
        }

        // Transfer organisation memberships (preserve role)
        PersonOrganisationRelation[] orgRelations = await GetAllPersonOrganisationRelationsAsync(predicate: r => r.PersonId == sourceId);

        foreach (PersonOrganisationRelation relation in orgRelations)
        {
            bool alreadyExists = await PersonOrganisationRelationExistsAsync(r => r.PersonId == survivorId && r.OrganisationId == relation.OrganisationId);

            if (!alreadyExists)
                await AddPersonToOrganisationAsync(survivorId, relation.Role, relation.OrganisationId);
        }

        // Transfer person-person relationships (preserve directionality and relation label)
        PersonRelationship[] personRelationships = await GetAllPersonRelationshipsAsync(predicate: r => r.SourcePersonId == sourceId || r.TargetPersonId == sourceId);

        foreach (PersonRelationship rel in personRelationships)
        {
            Guid newSource = rel.SourcePersonId == sourceId ? survivorId : rel.SourcePersonId;
            Guid newTarget = rel.TargetPersonId == sourceId ? survivorId : rel.TargetPersonId;

            if (newSource == newTarget) continue;

            bool alreadyExists = await PersonRelationshipExistsAsync(r => r.SourcePersonId == newSource && r.TargetPersonId == newTarget);

            if (!alreadyExists)
                await AddPersonRelationshipAsync(newSource, rel.Relation, newTarget);
        }

        // Delete source (cascades relation cleanup)
        await DeletePersonAsync(sourceId);

        if (startedTransaction) await Commit();
        return true;
    }

    // --- Organisation

    public async Task<bool> MergeOrganisationsAsync(Guid survivorId, Guid sourceId)
    {
        bool startedTransaction = await BeginTransaction();

        // Transfer authored resource relations
        ResourceAuthorRelation[] authoredResources = await GetAllResourceAuthorRelationsAsync(predicate: r => r.AuthorId == sourceId);

        foreach (ResourceAuthorRelation relation in authoredResources)
        {
            bool alreadyExists = await ResourceAuthorRelationExistsAsync(r => r.ResourceId == relation.ResourceId && r.AuthorId == survivorId);

            if (!alreadyExists)
                await AddAuthorToResourceAsync(relation.ResourceId, survivorId);
        }

        // Transfer resource-organisation relations (preserve role)
        ResourceOrganisationRelation[] resourceOrgRelations = await GetAllResourceOrganisationRelationsAsync(predicate: r => r.OrganisationId == sourceId);

        foreach (ResourceOrganisationRelation relation in resourceOrgRelations)
        {
            bool alreadyExists = await ResourceOrganisationRelationExistsAsync(r => r.ResourceId == relation.ResourceId && r.OrganisationId == survivorId);

            if (!alreadyExists)
            {
                if (relation.Role != null)
                    await AddOrganisationToResourceAsync(relation.ResourceId, survivorId, relation.Role);
                else
                    await AddOrganisationToResourceAsync(relation.ResourceId, survivorId);
            }
        }

        // Transfer person memberships (preserve role)
        PersonOrganisationRelation[] personRelations = await GetAllPersonOrganisationRelationsAsync(predicate: r => r.OrganisationId == sourceId);

        foreach (PersonOrganisationRelation relation in personRelations)
        {
            bool alreadyExists = await PersonOrganisationRelationExistsAsync(r => r.PersonId == relation.PersonId && r.OrganisationId == survivorId);

            if (!alreadyExists)
                await AddPersonToOrganisationAsync(relation.PersonId, relation.Role, survivorId);
        }

        // Transfer organisation-organisation relationships (preserve directionality and relation label)
        OrganisationRelationship[] orgRelationships = await GetAllOrganisationRelationshipsAsync(predicate: r => r.SourceOrganisationId == sourceId || r.TargetOrganisationId == sourceId);

        foreach (OrganisationRelationship rel in orgRelationships)
        {
            Guid newSource = rel.SourceOrganisationId == sourceId ? survivorId : rel.SourceOrganisationId;
            Guid newTarget = rel.TargetOrganisationId == sourceId ? survivorId : rel.TargetOrganisationId;

            if (newSource == newTarget) continue;

            bool alreadyExists = await OrganisationRelationshipExistsAsync(r => r.SourceOrganisationId == newSource && r.TargetOrganisationId == newTarget);

            if (!alreadyExists)
                await AddOrganisationRelationshipAsync(newSource, rel.Relation, newTarget);
        }

        // Delete source (cascades relation cleanup)
        await DeleteOrganisationAsync(sourceId);

        if (startedTransaction) await Commit();
        return true;
    }
}
