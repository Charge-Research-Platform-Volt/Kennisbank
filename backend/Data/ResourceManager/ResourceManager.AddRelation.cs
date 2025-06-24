// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using KnowledgeBank.Models;

namespace KnowledgeBank.Data
{
    // This part is for adding relations
    public partial class ResourceManager
    {
        #region Organisation-Organisation

        // Range
        public async Task AddOrganisationRelationshipRangeAsync(Guid sourceOrganisationId, string?[] relations, Guid[] targetOrganisationIds)
        {
            if (targetOrganisationIds.Length == 0) return;

            if (relations.Length != targetOrganisationIds.Length) throw new Exception("Relations and target organisation IDs array should be the same size");

            bool startedTransaction = await BeginTransaction();

            // Create entries
            OrganisationRelationship[] relationships = new OrganisationRelationship[targetOrganisationIds.Length];

            for (int i = 0; i < targetOrganisationIds.Length; i++)
            {
                relationships[i] = new()
                {
                    SourceOrganisationId = sourceOrganisationId,
                    Relation = relations[i],
                    TargetOrganisationId = targetOrganisationIds[i],
                };
            }

            // Add to database
            await database.OrganisationRelationships.AddRangeAsync(relationships);

            if (startedTransaction) await Commit();
        }

        public async Task AddOrganisationRelationshipRangeAsync(string sourceOrganisationId, string?[] relations, Guid[] targetOrganisationIds)
        { await AddOrganisationRelationshipRangeAsync(Guid.Parse(sourceOrganisationId), relations, targetOrganisationIds); }

        public async Task AddOrganisationRelationshipRangeAsync(Guid sourceOrganisationId, string?[] relations, string[] targetOrganisationIds)
        { await AddOrganisationRelationshipRangeAsync(sourceOrganisationId, relations, StringToGuidArray(targetOrganisationIds)); }

        public async Task AddOrganisationRelationshipRangeAsync(string sourceOrganisationId, string?[] relations, string[] targetOrganisationIds)
        { await AddOrganisationRelationshipRangeAsync(Guid.Parse(sourceOrganisationId), relations, StringToGuidArray(targetOrganisationIds)); }

        public async Task AddOrganisationRelationshipRangeAsync(Guid sourceOrganisationId, Guid[] targetOrganisationIds)
        { await AddOrganisationRelationshipRangeAsync(sourceOrganisationId, new string[targetOrganisationIds.Length], targetOrganisationIds); }

        public async Task AddOrganisationRelationshipAsync(string sourceOrganisationId, Guid[] targetOrganisationIds)
        { await AddOrganisationRelationshipRangeAsync(Guid.Parse(sourceOrganisationId), new string[targetOrganisationIds.Length], targetOrganisationIds); }

        public async Task AddOrganisationRelationshipAsync(Guid sourceOrganisationId, string[] targetOrganisationIds)
        { await AddOrganisationRelationshipRangeAsync(sourceOrganisationId, new string[targetOrganisationIds.Length], StringToGuidArray(targetOrganisationIds)); }

        public async Task AddOrganisationRelationshipAsync(string sourceOrganisationId, string[] targetOrganisationIds)
        { await AddOrganisationRelationshipRangeAsync(Guid.Parse(sourceOrganisationId), new string[targetOrganisationIds.Length], StringToGuidArray(targetOrganisationIds)); }

        // Single
        public async Task AddOrganisationRelationshipAsync(Guid sourceOrganisationId, string? relation, Guid targetOrganisationId)
        { await AddOrganisationRelationshipRangeAsync(sourceOrganisationId, [relation], [targetOrganisationId]); }

        public async Task AddOrganisationRelationshipAsync(string sourceOrganisationId, string? relation, Guid targetOrganisationId)
        { await AddOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), relation, targetOrganisationId); }

        public async Task AddOrganisationRelationshipAsync(Guid sourceOrganisationId, string? relation, string targetOrganistationId)
        { await AddOrganisationRelationshipAsync(sourceOrganisationId, relation, Guid.Parse(targetOrganistationId)); }

        public async Task AddOrganisationRelationshipAsync(string sourceOrganisationId, string? relation, string targetOrganisationId)
        { await AddOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), relation, Guid.Parse(targetOrganisationId)); }

        public async Task AddOrganisationRelationshipAsync(Guid sourceOrganisationId, Guid targetOrganisationId)
        { await AddOrganisationRelationshipRangeAsync(sourceOrganisationId, [targetOrganisationId]); }

        public async Task AddOrganisationRelationshipAsync(string sourceOrganisationId, Guid targetOrganisationId)
        { await AddOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), targetOrganisationId); }

        public async Task AddOrganisationRelationshipAsync(Guid sourceOrganisationId, string targetOrganistationId)
        { await AddOrganisationRelationshipAsync(sourceOrganisationId, Guid.Parse(targetOrganistationId)); }

        public async Task AddOrganisationRelationshipAsync(string sourceOrganisationId, string targetOrganisationId)
        { await AddOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), Guid.Parse(targetOrganisationId)); }

        #endregion

        #region Person-Organisation

        // Range
        public async Task AddPersonToOrganisationRangeAsync(Guid personId, string?[] roles, Guid[] organisationIds)
        {
            if (organisationIds.Length == 0) return;

            if (roles.Length != organisationIds.Length) throw new Exception("Roles array and organisationIds array should be the same size.");

            bool startedTransaction = await BeginTransaction();

            // Create entries
            PersonOrganisationRelation[] relations = new PersonOrganisationRelation[organisationIds.Length];

            for (int i = 0; i < organisationIds.Length; i++)
            {
                relations[i] = new()
                {
                    PersonId = personId,
                    Role = roles[i],
                    OrganisationId = organisationIds[i],
                };
            }

            // Add to database
            await database.PersonOrganisationRelations.AddRangeAsync(relations);

            if (startedTransaction) await Commit();
        }

        public async Task AddPersonToOrganisationRangeAsync(string personId, string?[] roles, Guid[] organisationIds)
        { await AddPersonToOrganisationRangeAsync(Guid.Parse(personId), roles, organisationIds); }

        public async Task AddPersonToOrganisationRangeAsync(Guid personId, string?[] roles, string[] organisationIds)
        { await AddPersonToOrganisationRangeAsync(personId, roles, StringToGuidArray(organisationIds)); }

        public async Task AddPersonToOrganisationRangeAsync(string personId, string?[] roles, string[] organisationIds)
        { await AddPersonToOrganisationRangeAsync(Guid.Parse(personId), roles, StringToGuidArray(organisationIds)); }

        public async Task AddPersonToOrganisationRangeAsync(Guid personId, Guid[] organisationIds)
        { await AddPersonToOrganisationRangeAsync(personId, new string[organisationIds.Length], organisationIds); }

        public async Task AddPersonToOrganisationRangeAsync(string personId, Guid[] organisationIds)
        { await AddPersonToOrganisationRangeAsync(Guid.Parse(personId), organisationIds); }

        public async Task AddPersonToOrganisationRangeAsync(Guid personId, string[] organisationIds)
        { await AddPersonToOrganisationRangeAsync(personId, organisationIds); }

        public async Task AddPersonToOrganisationRangeAsync(string personId, string[] organisationIds)
        { await AddPersonToOrganisationRangeAsync(Guid.Parse(personId), organisationIds); }

        // Single
        public async Task AddPersonToOrganisationAsync(Guid personId, string? role, Guid organisationId)
        { await AddPersonToOrganisationRangeAsync(personId, [role], [organisationId]); }

        public async Task AddPersonToOrganisationAsync(string personId, string? role, Guid organisationId)
        { await AddPersonToOrganisationAsync(Guid.Parse(personId), role, organisationId); }

        public async Task AddPersonToOrganisationAsync(Guid personId, string? role, string organisationId)
        { await AddPersonToOrganisationAsync(personId, role, Guid.Parse(organisationId)); }

        public async Task AddPersonToOrganisationAsync(string personId, string? role, string organisationId)
        { await AddPersonToOrganisationAsync(Guid.Parse(personId), role, Guid.Parse(organisationId)); }

        public async Task AddPersonToOrganisationAsync(Guid personId, Guid organisationId)
        { await AddPersonToOrganisationRangeAsync(personId, [organisationId]); }

        public async Task AddPersonToOrganisationAsync(string personId, Guid organisationId)
        { await AddPersonToOrganisationAsync(Guid.Parse(personId), organisationId); }

        public async Task AddPersonToOrganisationAsync(Guid personId, string organisationId)
        { await AddPersonToOrganisationAsync(personId, Guid.Parse(organisationId)); }

        public async Task AddPersonToOrganisationAsync(string personId, string organisationId)
        { await AddPersonToOrganisationAsync(Guid.Parse(personId), Guid.Parse(organisationId)); }

        #endregion

        #region Person-Person

        // Range
        public async Task AddPersonRelationshipRangeAsync(Guid sourcePersonId, string?[] relations, Guid[] targetPersonIds)
        {
            if (targetPersonIds.Length == 0) return;

            if (relations.Length != targetPersonIds.Length) throw new Exception("Relations and target person array should be the same length.");

            bool startedTransaction = await BeginTransaction();

            // Create entries
            PersonRelationship[] relationships = new PersonRelationship[targetPersonIds.Length];

            for (int i = 0; i < targetPersonIds.Length; i++)
            {
                relationships[i] = new()
                {
                    SourcePersonId = sourcePersonId,
                    Relation = relations[i],
                    TargetPersonId = targetPersonIds[i],
                };
            }

            // Add to database
            await database.PersonRelationships.AddRangeAsync(relationships);

            if (startedTransaction) await Commit();
        }

        public async Task AddPersonRelationshipRangeAsync(string sourcePersonId, string?[] relations, Guid[] targetPersonIds)
        { await AddPersonRelationshipRangeAsync(Guid.Parse(sourcePersonId), relations, targetPersonIds); }

        public async Task AddPersonRelationshipRangeAsync(Guid sourcePersonId, string?[] relations, string[] targetOrganistationIds)
        { await AddPersonRelationshipRangeAsync(sourcePersonId, relations, StringToGuidArray(targetOrganistationIds)); }

        public async Task AddPersonRelationshipRangeAsync(string sourcePersonId, string?[] relations, string[] targetPersonIds)
        { await AddPersonRelationshipRangeAsync(Guid.Parse(sourcePersonId), relations, StringToGuidArray(targetPersonIds)); }
        public async Task AddPersonRelationshipRangeAsync(Guid sourcePersonId, Guid[] targetPersonIds)
        { await AddPersonRelationshipRangeAsync(sourcePersonId, targetPersonIds); }

        public async Task AddPersonRelationshipRangeAsync(string sourcePersonId, Guid[] targetPersonIds)
        { await AddPersonRelationshipRangeAsync(Guid.Parse(sourcePersonId), targetPersonIds); }

        public async Task AddPersonRelationshipRangeAsync(Guid sourcePersonId, string[] targetOrganistationIds)
        { await AddPersonRelationshipRangeAsync(sourcePersonId, targetOrganistationIds); }

        public async Task AddPersonRelationshipRangeAsync(string sourcePersonId, string[] targetPersonIds)
        { await AddPersonRelationshipRangeAsync(Guid.Parse(sourcePersonId), targetPersonIds); }

        // Single
        public async Task AddPersonRelationshipAsync(Guid sourcePersonId, string? relation, Guid targetPersonId)
        { await AddPersonRelationshipRangeAsync(sourcePersonId, [relation], [targetPersonId]); }

        public async Task AddPersonRelationshipAsync(string sourcePersonId, string? relation, Guid targetPersonId)
        { await AddPersonRelationshipAsync(Guid.Parse(sourcePersonId), relation, targetPersonId); }

        public async Task AddPersonRelationshipAsync(Guid sourcePersonId, string? relation, string targetOrganistationId)
        { await AddPersonRelationshipAsync(sourcePersonId, relation, Guid.Parse(targetOrganistationId)); }

        public async Task AddPersonRelationshipAsync(string sourcePersonId, string? relation, string targetPersonId)
        { await AddPersonRelationshipAsync(Guid.Parse(sourcePersonId), relation, Guid.Parse(targetPersonId)); }
        public async Task AddPersonRelationshipAsync(Guid sourcePersonId, Guid targetPersonId)
        { await AddPersonRelationshipRangeAsync(sourcePersonId, [targetPersonId]); }

        public async Task AddPersonRelationshipAsync(string sourcePersonId, Guid targetPersonId)
        { await AddPersonRelationshipAsync(Guid.Parse(sourcePersonId), targetPersonId); }

        public async Task AddPersonRelationshipAsync(Guid sourcePersonId, string targetOrganistationId)
        { await AddPersonRelationshipAsync(sourcePersonId, Guid.Parse(targetOrganistationId)); }

        public async Task AddPersonRelationshipAsync(string sourcePersonId, string targetPersonId)
        { await AddPersonRelationshipAsync(Guid.Parse(sourcePersonId), Guid.Parse(targetPersonId)); }

        #endregion

        #region Resource-Person (Author)

        // Range
        public async Task AddAuthorToResourceRangeAsync(Guid resourceId, Guid[] authorIds)
        {
            if (authorIds.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            // Create entry
            ResourceAuthorRelation[] relations = new ResourceAuthorRelation[authorIds.Length];

            for (int i = 0; i < authorIds.Length; i++)
            {
                relations[i] = new()
                {
                    ResourceId = resourceId,
                    AuthorId = authorIds[i],
                };
            }

            // Add to database
            await database.ResourceAuthorRelations.AddRangeAsync(relations);

            if (startedTransaction) await Commit();
        }

        public async Task AddAuthorToResourceRangeAsync(Guid resourceId, string[] personIds)
        { await AddAuthorToResourceRangeAsync(resourceId, StringToGuidArray(personIds)); }

        public async Task AddAuthorToResourceRangeAsync(string resourceId, Guid[] personIds)
        { await AddAuthorToResourceRangeAsync(Guid.Parse(resourceId), personIds); }

        public async Task AddAuthorToResourceRangeAsync(string resourceId, string[] personIds)
        { await AddAuthorToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(personIds)); }

        // Single

        /// <summary>
        /// Adds a person as an author to a resource.
        /// 
        /// <example>
        /// For example:
        /// <code>
        /// await AddAuthorToResourceAsync(exampleResourceId, examplePersonId);
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="resourceId">The ID of the resource</param>
        /// <param name="personId">The ID of the person</param>
        /// <returns></returns>
        public async Task AddAuthorToResourceAsync(Guid resourceId, Guid personId)
        { await AddAuthorToResourceRangeAsync(resourceId, [personId]); }

        public async Task AddAuthorToResourceAsync(Guid resourceId, string personId)
        { await AddAuthorToResourceRangeAsync(resourceId, [personId]); }

        public async Task AddAuthorToResourceAsync(string resourceId, Guid personId)
        { await AddAuthorToResourceRangeAsync(resourceId, [personId]); }

        public async Task AddAuthorToResourceAsync(string resourceId, string personId)
        { await AddAuthorToResourceRangeAsync(resourceId, [personId]); }

        #endregion

        #region Resource-Organisation (Direct relation)

        // Range
        public async Task AddOrganisationToResourceRangeAsync(Guid resourceId, Guid[] organisationIds, string?[] roles)
        {
            if (organisationIds.Length == 0) return;

            if (organisationIds.Length != roles.Length) return;

            bool startedTransaction = await BeginTransaction();

            // Create entry
            ResourceOrganisationRelation[] relations = new ResourceOrganisationRelation[organisationIds.Length];

            for (int i = 0; i < organisationIds.Length; i++)
            {
                relations[i] = new()
                {
                    ResourceId = resourceId,
                    OrganisationId = organisationIds[i],
                    Role = roles[i],
                };
            }

            // Add to database
            await database.ResourceOrganisationRelations.AddRangeAsync(relations);

            if (startedTransaction) await Commit();
        }

        public async Task AddOrganisationToResourceRangeAsync(Guid resourceId, string[] organisationIds, string?[] roles)
        { await AddOrganisationToResourceRangeAsync(resourceId, StringToGuidArray(organisationIds), roles); }

        public async Task AddOrganisationToResourceRangeAsync(string resourceId, Guid[] organisationIds, string?[] roles)
        { await AddOrganisationToResourceRangeAsync(Guid.Parse(resourceId), organisationIds, roles); }

        public async Task AddOrganisationToResourceRangeAsync(string resourceId, string[] organisationIds, string?[] roles)
        { await AddOrganisationToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(organisationIds), roles); }

        public async Task AddOrganisationToResourceRangeAsync(Guid resourceId, Guid[] organisationIds)
        { await AddOrganisationToResourceRangeAsync(resourceId, organisationIds, new string[organisationIds.Length]); }

        public async Task AddOrganisationToResourceRangeAsync(Guid resourceId, string[] organisationIds)
        { await AddOrganisationToResourceRangeAsync(resourceId, StringToGuidArray(organisationIds), new string[organisationIds.Length]); }

        public async Task AddOrganisationToResourceRangeAsync(string resourceId, Guid[] organisationIds)
        { await AddOrganisationToResourceRangeAsync(Guid.Parse(resourceId), organisationIds, new string[organisationIds.Length]); }

        public async Task AddOrganisationToResourceRangeAsync(string resourceId, string[] organisationIds)
        { await AddOrganisationToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(organisationIds), new string[organisationIds.Length]); }

        // Single
        public async Task AddOrganisationToResourceAsync(Guid resourceId, Guid organisationId)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddOrganisationToResourceAsync(Guid resourceId, string organisationId)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddOrganisationToResourceAsync(string resourceId, Guid organisationId)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddOrganisationToResourceAsync(string resourceId, string organisationId)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddOrganisationToResourceAsync(Guid resourceId, Guid organisationId, string role)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        public async Task AddOrganisationToResourceAsync(Guid resourceId, string organisationId, string role)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        public async Task AddOrganisationToResourceAsync(string resourceId, Guid organisationId, string role)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        public async Task AddOrganisationToResourceAsync(string resourceId, string organisationId, string role)
        { await AddOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        #endregion

        #region Resource-Region

        // Range
        public async Task AddRegionToResourceRangeAsync(Guid resourceId, Guid[] regionIds)
        {
            if (regionIds.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            // Create entry
            ResourceRegionRelation[] relations = new ResourceRegionRelation[regionIds.Length];

            for (int i = 0; i < regionIds.Length; i++)
            {
                relations[i] = new()
                {
                    ResourceId = resourceId,
                    RegionId = regionIds[i],
                };
            }

            // Add to database
            await database.ResourceRegionRelations.AddRangeAsync(relations);

            if (startedTransaction) await Commit();
        }

        public async Task AddRegionToResourceRangeAsync(Guid resourceId, string[] regionIds)
        { await AddRegionToResourceRangeAsync(resourceId, StringToGuidArray(regionIds)); }

        public async Task AddRegionToResourceRangeAsync(string resourceId, Guid[] regionIds)
        { await AddRegionToResourceRangeAsync(Guid.Parse(resourceId), regionIds); }

        public async Task AddRegionToResourceRangeAsync(string resourceId, string[] regionIds)
        { await AddRegionToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(regionIds)); }

        // Single
        public async Task AddRegionToResourceAsync(Guid resourceId, Guid regionIds)
        { await AddRegionToResourceRangeAsync(resourceId, [regionIds]); }

        public async Task AddRegionToResourceAsync(Guid resourceId, string regionIds)
        { await AddRegionToResourceRangeAsync(resourceId, [regionIds]); }

        public async Task AddRegionToResourceAsync(string resourceId, Guid regionIds)
        { await AddRegionToResourceRangeAsync(resourceId, [regionIds]); }

        public async Task AddRegionToResourceAsync(string resourceId, string regionIds)
        { await AddRegionToResourceRangeAsync(resourceId, [regionIds]); }

        #endregion

        #region Resource-Source

        // Range
        public async Task AddSourceToResourceRangeAsync(Guid resourceId, string[] sourceUrls)
        {
            if (sourceUrls.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            // Create source entries
            ResourceSourceRelation[] sources = new ResourceSourceRelation[sourceUrls.Length];

            for (int i = 0; i < sourceUrls.Length; i++)
            {
                sources[i] = new()
                {
                    ResourceId = resourceId,
                    Url = sourceUrls[i],
                };
            }

            // Add to database
            await database.ResourceSourceRelations.AddRangeAsync(sources);

            if (startedTransaction) await Commit();
        }

        public async Task AddSourceToResourceRangeAsync(string resourceId, string[] sourceUrls)
        { await AddSourceToResourceRangeAsync(Guid.Parse(resourceId), sourceUrls); }

        // Single
        public async Task AddSourceToResourceAsync(Guid resourceId, string sourceUrl)
        { await AddSourceToResourceRangeAsync(resourceId, [sourceUrl]); }
        public async Task AddSourceToResourceAsync(string resourceId, string sourceUrl)
        { await AddSourceToResourceAsync(Guid.Parse(resourceId), sourceUrl); }

        #endregion

        #region Resource-Related Organisation (Indirect relation)

        // Range
        public async Task AddRelatedOrganisationToResourceRangeAsync(Guid resourceId, Guid[] organisationIds, string?[] roles)
        {
            if (organisationIds.Length == 0) return;

            if (organisationIds.Length != roles.Length) throw new Exception("The role and organisation ID arrays should be the same size!");

            bool startedTransaction = await BeginTransaction();

            // Create entry
            ResourceRelatedOrganisationRelation[] relations = new ResourceRelatedOrganisationRelation[organisationIds.Length];

            for (int i = 0; i < organisationIds.Length; i++)
            {
                relations[i] = new()
                {
                    ResourceId = resourceId,
                    OrganisationId = organisationIds[i],
                    Role = roles[i],
                };
            }

            // Add to database
            await database.ResourceRelatedOrganisationRelations.AddRangeAsync(relations);

            if (startedTransaction) await Commit();
        }

        public async Task AddRelatedOrganisationToResourceRangeAsync(Guid resourceId, string[] organisationIds, string?[] roles)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, StringToGuidArray(organisationIds), roles); }

        public async Task AddRelatedOrganisationToResourceRangeAsync(string resourceId, Guid[] organisationIds, string?[] roles)
        { await AddRelatedOrganisationToResourceRangeAsync(Guid.Parse(resourceId), organisationIds, roles); }

        public async Task AddRelatedOrganisationToResourceRangeAsync(string resourceId, string[] organisationIds, string?[] roles)
        { await AddRelatedOrganisationToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(organisationIds), roles); }

        public async Task AddRelatedOrganisationToResourceRangeAsync(Guid resourceId, Guid[] organisationIds)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, organisationIds, new string[organisationIds.Length]); }

        public async Task AddRelatedOrganisationToResourceRangeAsync(Guid resourceId, string[] organisationIds)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, StringToGuidArray(organisationIds), new string[organisationIds.Length]); }

        public async Task AddRelatedOrganisationToResourceRangeAsync(string resourceId, Guid[] organisationIds)
        { await AddRelatedOrganisationToResourceRangeAsync(Guid.Parse(resourceId), organisationIds, new string[organisationIds.Length]); }

        public async Task AddRelatedOrganisationToResourceRangeAsync(string resourceId, string[] organisationIds)
        { await AddRelatedOrganisationToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(organisationIds), new string[organisationIds.Length]); }

        // Single
        public async Task AddRelatedOrganisationToResourceAsync(Guid resourceId, Guid organisationId)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddRelatedOrganisationToResourceAsync(Guid resourceId, string organisationId)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddRelatedOrganisationToResourceAsync(string resourceId, Guid organisationId)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddRelatedOrganisationToResourceAsync(string resourceId, string organisationId)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId]); }

        public async Task AddRelatedOrganisationToResourceAsync(Guid resourceId, Guid organisationId, string role)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        public async Task AddRelatedOrganisationToResourceAsync(Guid resourceId, string organisationId, string role)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        public async Task AddRelatedOrganisationToResourceAsync(string resourceId, Guid organisationId, string role)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        public async Task AddRelatedOrganisationToResourceAsync(string resourceId, string organisationId, string role)
        { await AddRelatedOrganisationToResourceRangeAsync(resourceId, [organisationId], [role]); }

        #endregion

        #region Resource-Related Person (Non-author)

        // Range
        public async Task AddRelatedPersonToResourceRangeAsync(Guid resourceId, Guid[] personIds, string?[] roles)
        {
            if (personIds.Length == 0) return;

            if (personIds.Length != roles.Length) throw new Exception("The person ID and role arrays should be the same size.");

            bool startedTransaction = await BeginTransaction();

            // Create entry
            ResourceRelatedPersonRelation[] relations = new ResourceRelatedPersonRelation[personIds.Length];

            for (int i = 0; i < personIds.Length; i++)
            {
                relations[i] = new()
                {
                    ResourceId = resourceId,
                    PersonId = personIds[i],
                    Role = roles[i],
                };
            }

            // Add to database
            await database.ResourceRelatedPersonRelations.AddRangeAsync(relations);

            if (startedTransaction) await Commit();
        }

        public async Task AddRelatedPersonToResourceRangeAsync(Guid resourceId, string[] personIds, string?[] roles)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, StringToGuidArray(personIds), roles); }

        public async Task AddRelatedPersonToResourceRangeAsync(string resourceId, Guid[] personIds, string?[] roles)
        { await AddRelatedPersonToResourceRangeAsync(Guid.Parse(resourceId), personIds, roles); }

        public async Task AddRelatedPersonToResourceRangeAsync(string resourceId, string[] personIds, string?[] roles)
        { await AddRelatedPersonToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(personIds), roles); }

        public async Task AddRelatedPersonToResourceRangeAsync(Guid resourceId, Guid[] personIds)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, personIds, new string[personIds.Length]); }

        public async Task AddRelatedPersonToResourceRangeAsync(Guid resourceId, string[] personIds)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, StringToGuidArray(personIds)); }

        public async Task AddRelatedPersonToResourceRangeAsync(string resourceId, Guid[] personIds)
        { await AddRelatedPersonToResourceRangeAsync(Guid.Parse(resourceId), personIds); }

        public async Task AddRelatedPersonToResourceRangeAsync(string resourceId, string[] personIds)
        { await AddRelatedPersonToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(personIds)); }

        // Single
        public async Task AddRelatedPersonToResourceAsync(Guid resourceId, Guid personId, string? role)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, [personId], [role]); }

        public async Task AddRelatedPersonToResourceAsync(Guid resourceId, string personId, string? role)
        { await AddRelatedPersonToResourceAsync(resourceId, personId, role); }

        public async Task AddRelatedPersonToResourceAsync(string resourceId, Guid personId, string? role)
        { await AddRelatedPersonToResourceAsync(resourceId, personId, role); }

        public async Task AddRelatedPersonToResourceAsync(string resourceId, string personId, string? role)
        { await AddRelatedPersonToResourceAsync(resourceId, personId, role); }

        public async Task AddRelatedPersonToResourceAsync(Guid resourceId, Guid personId)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, [personId]); }

        public async Task AddRelatedPersonToResourceAsync(Guid resourceId, string personId)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, [personId]); }

        public async Task AddRelatedPersonToResourceAsync(string resourceId, Guid personId)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, [personId]); }

        public async Task AddRelatedPersonToResourceAsync(string resourceId, string personId)
        { await AddRelatedPersonToResourceRangeAsync(resourceId, [personId]); }

        #endregion

        #region Resource-Related Source (not actual source)

        // Range
        public async Task AddRelatedSourceToResourceRangeAsync(Guid resourceId, string[] urls)
        {
            if (urls.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            // Create entries
            ResourceRelatedSourceRelation[] relations = new ResourceRelatedSourceRelation[urls.Length];

            for (int i = 0; i < urls.Length; i++)
            {
                relations[i] = new()
                {
                    ResourceId = resourceId,
                    Url = urls[i],
                };
            }

            // Add to database
            await database.ResourceRelatedSourceRelations.AddRangeAsync(relations);

            if (startedTransaction) await Commit();
        }

        public async Task AddRelatedSourceToResourceRangeAsync(string resourceId, string[] urls)
        { await AddRelatedSourceToResourceRangeAsync(Guid.Parse(resourceId), urls); }

        // Single
        public async Task AddRelatedSourceToResourceAsync(Guid resourceId, string url)
        { await AddRelatedSourceToResourceRangeAsync(resourceId, [url]); }

        public async Task AddRelatedSourceToResourceAsync(string resourceId, string url)
        { await AddRelatedSourceToResourceAsync(Guid.Parse(resourceId), url); }

        #endregion

        #region Resource-Tag

        // Range
        public async Task AddTagToResourceRangeAsync(Guid resourceId, Guid[] tagIds)
        {
            if (tagIds.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            ResourceTagRelation[] tagRelations = new ResourceTagRelation[tagIds.Length];

            for (int i = 0; i < tagIds.Length; i++)
            {
                tagRelations[i] = new()
                {
                    ResourceId = resourceId,
                    TagId = tagIds[i]
                };
            }

            await database.ResourceTagRelations.AddRangeAsync(tagRelations);

            if (startedTransaction) await Commit();
        }

        public async Task AddTagToResourceRangeAsync(Guid resourceId, string[] tagIds)
        { await AddTagToResourceRangeAsync(resourceId, StringToGuidArray(tagIds)); }

        public async Task AddTagToResourceRangeAsync(string resourceId, Guid[] tagIds)
        { await AddTagToResourceRangeAsync(Guid.Parse(resourceId), tagIds); }

        public async Task AddTagToResourceRangeAsync(string resourceId, string[] tagIds)
        { await AddTagToResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(tagIds)); }

        // Single
        public async Task AddTagToResourceAsync(Guid resourceId, Guid tagId)
        { await AddTagToResourceRangeAsync(resourceId, [tagId]); }

        public async Task AddTagToResourceAsync(string resourceId, Guid tagId)
        { await AddTagToResourceAsync(Guid.Parse(resourceId), tagId); }

        public async Task AddTagToResourceAsync(Guid resourceId, string tagId)
        { await AddTagToResourceAsync(resourceId, Guid.Parse(tagId)); }

        public async Task AddTagToResourceAsync(string resourceId, string tagId)
        { await AddTagToResourceAsync(Guid.Parse(resourceId), Guid.Parse(tagId)); }

        #endregion

        #region Resource-ResourceType

        public async Task AddResourceTypeToResourceAsync(Guid resourceId, Guid resourceTypeId)
        {
            bool startedTransaction = await BeginTransaction();

            Resource? resource = await GetResourceAsync(resourceId);

            if (resource == null) return;

            resource.TypeId = resourceTypeId;

            if (startedTransaction) await Commit();
        }

        public async Task AddResourceTypeToResourceAsync(Guid resourceId, string resourceTypeId)
        { await AddResourceTypeToResourceAsync(resourceId, Guid.Parse(resourceTypeId)); }

        public async Task AddResourceTypeToResourceAsync(string resourceId, Guid resourceTypeId)
        { await AddResourceTypeToResourceAsync(Guid.Parse(resourceId), resourceTypeId); }

        public async Task AddResourceTypeToResourceAsync(string resourceId, string resourceTypeId)
        { await AddResourceTypeToResourceAsync(Guid.Parse(resourceId), Guid.Parse(resourceTypeId)); }

        #endregion

        #region Resource-AudioMetadata

        public async Task AddAudioMetadataToResourceAsync(Guid resourceId)
        {
            if (await GetAudioMetadataAsync(resourceId) != null) return;

            bool startedTransaction = await BeginTransaction();

            await database.AudioMetadata.AddAsync(new()
            {
                ResourceId = resourceId
            });

            if (startedTransaction) await Commit();
        }

        public async Task AddAudioMetadataToResourceAsync(string resourceId)
        { await AddAudioMetadataToResourceAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-VideoMetadata

        public async Task AddVideoMetadataToResourceAsync(Guid resourceId)
        {
            if (await GetVideoMetadataAsync(resourceId) != null) return;

            bool startedTransaction = await BeginTransaction();

            await database.VideoMetadata.AddAsync(new()
            {
                ResourceId = resourceId
            });

            if (startedTransaction) await Commit();
        }

        public async Task AddVideoMetadataToResourceAsync(string resourceId)
        { await AddVideoMetadataToResourceAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-DocumentMetadata

        public async Task AddDocumentMetadataToResourceAsync(Guid resourceId)
        {
            if (await GetDocumentMetadataAsync(resourceId) != null) return;

            bool startedTransaction = await BeginTransaction();

            await database.DocumentMetadata.AddAsync(new()
            {
                ResourceId = resourceId
            });

            if (startedTransaction) await Commit();
        }

        public async Task AddDocumentMetadataToResourceAsync(string resourceId)
        { await AddDocumentMetadataToResourceAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Resource-WebsiteMetadata

        public async Task AddWebsiteMetadataToResourceAsync(Guid resourceId, string url)
        {
            if (await GetWebsiteMetadataAsync(resourceId) != null) return;

            bool startedTransaction = await BeginTransaction();

            await database.WebsiteMetadata.AddAsync(new()
            {
                ResourceId = resourceId,
                Url = url
            });

            if (startedTransaction) await Commit();
        }

        public async Task AddWebsiteMetadataToResourceAsync(string resourceId, string url)
        { await AddWebsiteMetadataToResourceAsync(Guid.Parse(resourceId), url); }

        #endregion
    }
}