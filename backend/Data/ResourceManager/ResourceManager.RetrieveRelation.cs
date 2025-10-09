// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using KnowledgeBank.Models;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    // This part is for retrieving resources or their properties
    public partial class ResourceManager
    {
        #region OrganisationRelationship

        // OrganisationRelationship itself

        private readonly Expression<Func<OrganisationRelationship, object>> organisationRelationshipDefaultOrderBy = organisationRelationship => organisationRelationship.SourceOrganisationId;
        private const bool organisationRelationshipDefaultOrderDescending = true;


        // Single

        public async Task<OrganisationRelationship?> GetOrganisationRelationshipAsync(Expression<Func<OrganisationRelationship, bool>> predicate, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;

            return await GetAsync(database.OrganisationRelationships, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<OrganisationRelationship?> GetOrganisationRelationshipAsync(Expression<Func<OrganisationRelationship, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.OrganisationRelationships, predicate, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<OrganisationRelationship[]> GetAllOrganisationRelationshipsAsync(Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, Expression<Func<OrganisationRelationship, bool>>? predicate = null)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;

            return await GetAllAsync(database.OrganisationRelationships, orderBy, orderDescending, predicate);
        }

        public async Task<OrganisationRelationship[]> GetAllOrganisationRelationshipsAsync(Expression<Func<OrganisationRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.OrganisationRelationships, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<OrganisationRelationship[]> GetAllOrganisationRelationshipsAsync(Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.OrganisationRelationships, orderBy, orderDescending, null, includeProperties); }

        public async Task<OrganisationRelationship[]> GetAllOrganisationRelationshipsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.OrganisationRelationships, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, null, includeProperties); }

        public async Task<OrganisationRelationship[]> GetOrganisationRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, Expression<Func<OrganisationRelationship, bool>>? predicate = null)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;

            return await GetPageAsync(database.OrganisationRelationships, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<OrganisationRelationship[]> GetOrganisationRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<OrganisationRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.OrganisationRelationships, pageIndex, pageSize, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<OrganisationRelationship[]> GetOrganisationRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;

            return await GetPageAsync(database.OrganisationRelationships, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<OrganisationRelationship[]> GetOrganisationRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.OrganisationRelationships, pageIndex, pageSize, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllOrganisationRelationshipsAsync(string projection, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, Expression<Func<OrganisationRelationship, bool>>? predicate = null)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;
            return await GetAllAsync(database.OrganisationRelationships, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllOrganisationRelationshipsAsync(string projection, Expression<Func<OrganisationRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.OrganisationRelationships, projection, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllOrganisationRelationshipsAsync(string projection, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;
            return await GetAllAsync(database.OrganisationRelationships, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllOrganisationRelationshipsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.OrganisationRelationships, projection, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetOrganisationRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, Expression<Func<OrganisationRelationship, bool>>? predicate = null)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;
            return await GetPageAsync(database.OrganisationRelationships, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetOrganisationRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<OrganisationRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.OrganisationRelationships, projection, pageIndex, pageSize, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetOrganisationRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;
            return await GetPageAsync(database.OrganisationRelationships, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetOrganisationRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.OrganisationRelationships, projection, pageIndex, pageSize, organisationRelationshipDefaultOrderBy, organisationRelationshipDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetOrganisationRelationshipPropertyAsync(Expression<Func<OrganisationRelationship, bool>> predicate, string selector, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;
            return await GetPropertyAsync(database.OrganisationRelationships, predicate, selector, orderBy, orderDescending);
        }




        public async Task<dynamic?> GetOrganisationRelationshipPropertyOrDefaultAsync(Expression<Func<OrganisationRelationship, bool>> predicate, string selector, Expression<Func<OrganisationRelationship, object>>? orderBy = null, bool orderDescending = organisationRelationshipDefaultOrderDescending)
        {
            orderBy ??= organisationRelationshipDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.OrganisationRelationships, predicate, selector, orderBy, orderDescending);
        }

        #endregion


        #region PersonOrganisationRelation

        // PersonOrganisationRelation itself

        private readonly Expression<Func<PersonOrganisationRelation, object>> personOrganisationRelationDefaultOrderBy = personOrganisationRelation => personOrganisationRelation.PersonId;
        private const bool personOrganisationRelationDefaultOrderDescending = true;


        // Single

        public async Task<PersonOrganisationRelation?> GetPersonOrganisationRelationAsync(Expression<Func<PersonOrganisationRelation, bool>> predicate, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;

            return await GetAsync(database.PersonOrganisationRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<PersonOrganisationRelation?> GetPersonOrganisationRelationAsync(Expression<Func<PersonOrganisationRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.PersonOrganisationRelations, predicate, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<PersonOrganisationRelation[]> GetAllPersonOrganisationRelationsAsync(Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, Expression<Func<PersonOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;

            return await GetAllAsync(database.PersonOrganisationRelations, orderBy, orderDescending, predicate);
        }

        public async Task<PersonOrganisationRelation[]> GetAllPersonOrganisationRelationsAsync(Expression<Func<PersonOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.PersonOrganisationRelations, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<PersonOrganisationRelation[]> GetAllPersonOrganisationRelationsAsync(Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.PersonOrganisationRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<PersonOrganisationRelation[]> GetAllPersonOrganisationRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.PersonOrganisationRelations, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<PersonOrganisationRelation[]> GetPersonOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, Expression<Func<PersonOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;

            return await GetPageAsync(database.PersonOrganisationRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<PersonOrganisationRelation[]> GetPersonOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<PersonOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.PersonOrganisationRelations, pageIndex, pageSize, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<PersonOrganisationRelation[]> GetPersonOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;

            return await GetPageAsync(database.PersonOrganisationRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<PersonOrganisationRelation[]> GetPersonOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.PersonOrganisationRelations, pageIndex, pageSize, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllPersonOrganisationRelationsAsync(string projection, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, Expression<Func<PersonOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;
            return await GetAllAsync(database.PersonOrganisationRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllPersonOrganisationRelationsAsync(string projection, Expression<Func<PersonOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.PersonOrganisationRelations, projection, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllPersonOrganisationRelationsAsync(string projection, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;
            return await GetAllAsync(database.PersonOrganisationRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllPersonOrganisationRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.PersonOrganisationRelations, projection, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetPersonOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, Expression<Func<PersonOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;
            return await GetPageAsync(database.PersonOrganisationRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetPersonOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<PersonOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.PersonOrganisationRelations, projection, pageIndex, pageSize, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetPersonOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;
            return await GetPageAsync(database.PersonOrganisationRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetPersonOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.PersonOrganisationRelations, projection, pageIndex, pageSize, personOrganisationRelationDefaultOrderBy, personOrganisationRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetPersonOrganisationRelationPropertyAsync(Expression<Func<PersonOrganisationRelation, bool>> predicate, string selector, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;
            return await GetPropertyAsync(database.PersonOrganisationRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetPersonOrganisationRelationPropertyOrDefaultAsync(Expression<Func<PersonOrganisationRelation, bool>> predicate, string selector, Expression<Func<PersonOrganisationRelation, object>>? orderBy = null, bool orderDescending = personOrganisationRelationDefaultOrderDescending)
        {
            orderBy ??= personOrganisationRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.PersonOrganisationRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region PersonRelationship

        // PersonRelationship itself

        private readonly Expression<Func<PersonRelationship, object>> personRelationshipDefaultOrderBy = personRelationship => personRelationship.SourcePersonId;
        private const bool personRelationshipDefaultOrderDescending = true;


        // Single

        public async Task<PersonRelationship?> GetPersonRelationshipAsync(Expression<Func<PersonRelationship, bool>> predicate, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personRelationshipDefaultOrderBy;

            return await GetAsync(database.PersonRelationships, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<PersonRelationship?> GetPersonRelationshipAsync(Expression<Func<PersonRelationship, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.PersonRelationships, predicate, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<PersonRelationship[]> GetAllPersonRelationshipsAsync(Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, Expression<Func<PersonRelationship, bool>>? predicate = null)
        {
            orderBy ??= personRelationshipDefaultOrderBy;

            return await GetAllAsync(database.PersonRelationships, orderBy, orderDescending, predicate);
        }

        public async Task<PersonRelationship[]> GetAllPersonRelationshipsAsync(Expression<Func<PersonRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.PersonRelationships, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<PersonRelationship[]> GetAllPersonRelationshipsAsync(Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.PersonRelationships, orderBy, orderDescending, null, includeProperties); }

        public async Task<PersonRelationship[]> GetAllPersonRelationshipsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.PersonRelationships, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, null, includeProperties); }

        public async Task<PersonRelationship[]> GetPersonRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, Expression<Func<PersonRelationship, bool>>? predicate = null)
        {
            orderBy ??= personRelationshipDefaultOrderBy;

            return await GetPageAsync(database.PersonRelationships, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<PersonRelationship[]> GetPersonRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<PersonRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.PersonRelationships, pageIndex, pageSize, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<PersonRelationship[]> GetPersonRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personRelationshipDefaultOrderBy;

            return await GetPageAsync(database.PersonRelationships, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<PersonRelationship[]> GetPersonRelationshipPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.PersonRelationships, pageIndex, pageSize, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllPersonRelationshipsAsync(string projection, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, Expression<Func<PersonRelationship, bool>>? predicate = null)
        {
            orderBy ??= personRelationshipDefaultOrderBy;
            return await GetAllAsync(database.PersonRelationships, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllPersonRelationshipsAsync(string projection, Expression<Func<PersonRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.PersonRelationships, projection, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllPersonRelationshipsAsync(string projection, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personRelationshipDefaultOrderBy;
            return await GetAllAsync(database.PersonRelationships, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllPersonRelationshipsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.PersonRelationships, projection, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetPersonRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, Expression<Func<PersonRelationship, bool>>? predicate = null)
        {
            orderBy ??= personRelationshipDefaultOrderBy;
            return await GetPageAsync(database.PersonRelationships, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetPersonRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<PersonRelationship, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.PersonRelationships, projection, pageIndex, pageSize, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetPersonRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personRelationshipDefaultOrderBy;
            return await GetPageAsync(database.PersonRelationships, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetPersonRelationshipPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.PersonRelationships, projection, pageIndex, pageSize, personRelationshipDefaultOrderBy, personRelationshipDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetPersonRelationshipPropertyAsync(Expression<Func<PersonRelationship, bool>> predicate, string selector, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending)
        {
            orderBy ??= personRelationshipDefaultOrderBy;
            return await GetPropertyAsync(database.PersonRelationships, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetPersonRelationshipPropertyOrDefaultAsync(Expression<Func<PersonRelationship, bool>> predicate, string selector, Expression<Func<PersonRelationship, object>>? orderBy = null, bool orderDescending = personRelationshipDefaultOrderDescending)
        {
            orderBy ??= personRelationshipDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.PersonRelationships, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region ResourceAuthorRelation

        // ResourceAuthorRelation itself

        private readonly Expression<Func<ResourceAuthorRelation, object>> resourceAuthorRelationDefaultOrderBy = resourceAuthorRelation => resourceAuthorRelation.ResourceId;
        private const bool resourceAuthorRelationDefaultOrderDescending = true;


        // Single

        public async Task<ResourceAuthorRelation?> GetResourceAuthorRelationAsync(Expression<Func<ResourceAuthorRelation, bool>> predicate, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;

            return await GetAsync(database.ResourceAuthorRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<ResourceAuthorRelation?> GetResourceAuthorRelationAsync(Expression<Func<ResourceAuthorRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceAuthorRelations, predicate, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<ResourceAuthorRelation[]> GetAllResourceAuthorRelationsAsync(Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, Expression<Func<ResourceAuthorRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;

            return await GetAllAsync(database.ResourceAuthorRelations, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceAuthorRelation[]> GetAllResourceAuthorRelationsAsync(Expression<Func<ResourceAuthorRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceAuthorRelations, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceAuthorRelation[]> GetAllResourceAuthorRelationsAsync(Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceAuthorRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceAuthorRelation[]> GetAllResourceAuthorRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceAuthorRelations, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceAuthorRelation[]> GetResourceAuthorRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, Expression<Func<ResourceAuthorRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceAuthorRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceAuthorRelation[]> GetResourceAuthorRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceAuthorRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceAuthorRelations, pageIndex, pageSize, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceAuthorRelation[]> GetResourceAuthorRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceAuthorRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceAuthorRelation[]> GetResourceAuthorRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceAuthorRelations, pageIndex, pageSize, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllResourceAuthorRelationsAsync(string projection, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, Expression<Func<ResourceAuthorRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceAuthorRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllResourceAuthorRelationsAsync(string projection, Expression<Func<ResourceAuthorRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceAuthorRelations, projection, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllResourceAuthorRelationsAsync(string projection, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceAuthorRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllResourceAuthorRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceAuthorRelations, projection, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetResourceAuthorRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, Expression<Func<ResourceAuthorRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceAuthorRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetResourceAuthorRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceAuthorRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceAuthorRelations, projection, pageIndex, pageSize, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetResourceAuthorRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceAuthorRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetResourceAuthorRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceAuthorRelations, projection, pageIndex, pageSize, resourceAuthorRelationDefaultOrderBy, resourceAuthorRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetResourceAuthorRelationPropertyAsync(Expression<Func<ResourceAuthorRelation, bool>> predicate, string selector, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceAuthorRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetResourceAuthorRelationPropertyOrDefaultAsync(Expression<Func<ResourceAuthorRelation, bool>> predicate, string selector, Expression<Func<ResourceAuthorRelation, object>>? orderBy = null, bool orderDescending = resourceAuthorRelationDefaultOrderDescending)
        {
            orderBy ??= resourceAuthorRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceAuthorRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region ResourceOrganisationRelation

        // ResourceOrganisationRelation itself

        private readonly Expression<Func<ResourceOrganisationRelation, object>> resourceOrganisationRelationDefaultOrderBy = resourceOrganisationRelation => resourceOrganisationRelation.ResourceId;
        private const bool resourceOrganisationRelationDefaultOrderDescending = true;


        // Single

        public async Task<ResourceOrganisationRelation?> GetResourceOrganisationRelationAsync(Expression<Func<ResourceOrganisationRelation, bool>> predicate, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;

            return await GetAsync(database.ResourceOrganisationRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<ResourceOrganisationRelation?> GetResourceOrganisationRelationAsync(Expression<Func<ResourceOrganisationRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceOrganisationRelations, predicate, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<ResourceOrganisationRelation[]> GetAllResourceOrganisationRelationsAsync(Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;

            return await GetAllAsync(database.ResourceOrganisationRelations, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceOrganisationRelation[]> GetAllResourceOrganisationRelationsAsync(Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceOrganisationRelations, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceOrganisationRelation[]> GetAllResourceOrganisationRelationsAsync(Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceOrganisationRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceOrganisationRelation[]> GetAllResourceOrganisationRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceOrganisationRelations, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceOrganisationRelation[]> GetResourceOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceOrganisationRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceOrganisationRelation[]> GetResourceOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceOrganisationRelations, pageIndex, pageSize, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceOrganisationRelation[]> GetResourceOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceOrganisationRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceOrganisationRelation[]> GetResourceOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceOrganisationRelations, pageIndex, pageSize, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllResourceOrganisationRelationsAsync(string projection, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceOrganisationRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllResourceOrganisationRelationsAsync(string projection, Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceOrganisationRelations, projection, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllResourceOrganisationRelationsAsync(string projection, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceOrganisationRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllResourceOrganisationRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceOrganisationRelations, projection, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetResourceOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceOrganisationRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetResourceOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceOrganisationRelations, projection, pageIndex, pageSize, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetResourceOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceOrganisationRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetResourceOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceOrganisationRelations, projection, pageIndex, pageSize, resourceOrganisationRelationDefaultOrderBy, resourceOrganisationRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetResourceOrganisationRelationPropertyAsync(Expression<Func<ResourceOrganisationRelation, bool>> predicate, string selector, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceOrganisationRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetResourceOrganisationRelationPropertyOrDefaultAsync(Expression<Func<ResourceOrganisationRelation, bool>> predicate, string selector, Expression<Func<ResourceOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceOrganisationRelationDefaultOrderDescending)
        {
            orderBy ??= resourceOrganisationRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceOrganisationRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region ResourceRegionRelation

        // ResourceRegionRelation itself

        private readonly Expression<Func<ResourceRegionRelation, object>> resourceRegionRelationDefaultOrderBy = resourceRegionRelation => resourceRegionRelation.ResourceId;
        private const bool resourceRegionRelationDefaultOrderDescending = true;


        // Single

        public async Task<ResourceRegionRelation?> GetResourceRegionRelationAsync(Expression<Func<ResourceRegionRelation, bool>> predicate, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;

            return await GetAsync(database.ResourceRegionRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<ResourceRegionRelation?> GetResourceRegionRelationAsync(Expression<Func<ResourceRegionRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceRegionRelations, predicate, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<ResourceRegionRelation[]> GetAllResourceRegionRelationsAsync(Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, Expression<Func<ResourceRegionRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;

            return await GetAllAsync(database.ResourceRegionRelations, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceRegionRelation[]> GetAllResourceRegionRelationsAsync(Expression<Func<ResourceRegionRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRegionRelations, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceRegionRelation[]> GetAllResourceRegionRelationsAsync(Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRegionRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceRegionRelation[]> GetAllResourceRegionRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRegionRelations, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceRegionRelation[]> GetResourceRegionRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, Expression<Func<ResourceRegionRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceRegionRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceRegionRelation[]> GetResourceRegionRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRegionRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRegionRelations, pageIndex, pageSize, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceRegionRelation[]> GetResourceRegionRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceRegionRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceRegionRelation[]> GetResourceRegionRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRegionRelations, pageIndex, pageSize, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllResourceRegionRelationsAsync(string projection, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, Expression<Func<ResourceRegionRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceRegionRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllResourceRegionRelationsAsync(string projection, Expression<Func<ResourceRegionRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRegionRelations, projection, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllResourceRegionRelationsAsync(string projection, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceRegionRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllResourceRegionRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRegionRelations, projection, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetResourceRegionRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, Expression<Func<ResourceRegionRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceRegionRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetResourceRegionRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRegionRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRegionRelations, projection, pageIndex, pageSize, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetResourceRegionRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceRegionRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetResourceRegionRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRegionRelations, projection, pageIndex, pageSize, resourceRegionRelationDefaultOrderBy, resourceRegionRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetResourceRegionRelationPropertyAsync(Expression<Func<ResourceRegionRelation, bool>> predicate, string selector, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceRegionRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetResourceRegionRelationPropertyOrDefaultAsync(Expression<Func<ResourceRegionRelation, bool>> predicate, string selector, Expression<Func<ResourceRegionRelation, object>>? orderBy = null, bool orderDescending = resourceRegionRelationDefaultOrderDescending)
        {
            orderBy ??= resourceRegionRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceRegionRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region ResourceRelatedOrganisationRelation

        // ResourceRelatedOrganisationRelation itself

        private readonly Expression<Func<ResourceRelatedOrganisationRelation, object>> resourceRelatedOrganisationRelationDefaultOrderBy = resourceRelatedOrganisationRelation => resourceRelatedOrganisationRelation.ResourceId;
        private const bool resourceRelatedOrganisationRelationDefaultOrderDescending = true;


        // Single

        public async Task<ResourceRelatedOrganisationRelation?> GetResourceRelatedOrganisationRelationAsync(Expression<Func<ResourceRelatedOrganisationRelation, bool>> predicate, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;

            return await GetAsync(database.ResourceRelatedOrganisationRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<ResourceRelatedOrganisationRelation?> GetResourceRelatedOrganisationRelationAsync(Expression<Func<ResourceRelatedOrganisationRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceRelatedOrganisationRelations, predicate, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<ResourceRelatedOrganisationRelation[]> GetAllResourceRelatedOrganisationRelationsAsync(Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;

            return await GetAllAsync(database.ResourceRelatedOrganisationRelations, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceRelatedOrganisationRelation[]> GetAllResourceRelatedOrganisationRelationsAsync(Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedOrganisationRelations, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceRelatedOrganisationRelation[]> GetAllResourceRelatedOrganisationRelationsAsync(Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedOrganisationRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceRelatedOrganisationRelation[]> GetAllResourceRelatedOrganisationRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedOrganisationRelations, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceRelatedOrganisationRelation[]> GetResourceRelatedOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceRelatedOrganisationRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceRelatedOrganisationRelation[]> GetResourceRelatedOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedOrganisationRelations, pageIndex, pageSize, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceRelatedOrganisationRelation[]> GetResourceRelatedOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceRelatedOrganisationRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceRelatedOrganisationRelation[]> GetResourceRelatedOrganisationRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedOrganisationRelations, pageIndex, pageSize, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllResourceRelatedOrganisationRelationsAsync(string projection, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceRelatedOrganisationRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllResourceRelatedOrganisationRelationsAsync(string projection, Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedOrganisationRelations, projection, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllResourceRelatedOrganisationRelationsAsync(string projection, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceRelatedOrganisationRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllResourceRelatedOrganisationRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedOrganisationRelations, projection, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetResourceRelatedOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceRelatedOrganisationRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetResourceRelatedOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedOrganisationRelations, projection, pageIndex, pageSize, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetResourceRelatedOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceRelatedOrganisationRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetResourceRelatedOrganisationRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedOrganisationRelations, projection, pageIndex, pageSize, resourceRelatedOrganisationRelationDefaultOrderBy, resourceRelatedOrganisationRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetResourceRelatedOrganisationRelationPropertyAsync(Expression<Func<ResourceRelatedOrganisationRelation, bool>> predicate, string selector, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceRelatedOrganisationRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetResourceRelatedOrganisationRelationPropertyOrDefaultAsync(Expression<Func<ResourceRelatedOrganisationRelation, bool>> predicate, string selector, Expression<Func<ResourceRelatedOrganisationRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedOrganisationRelationDefaultOrderDescending)
        {
            orderBy ??= resourceRelatedOrganisationRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceRelatedOrganisationRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region ResourceRelatedPersonRelation

        // ResourceRelatedPersonRelation itself

        private readonly Expression<Func<ResourceRelatedPersonRelation, object>> resourceRelatedPersonRelationDefaultOrderBy = resourceRelatedPersonRelation => resourceRelatedPersonRelation.ResourceId;
        private const bool resourceRelatedPersonRelationDefaultOrderDescending = true;


        // Single

        public async Task<ResourceRelatedPersonRelation?> GetResourceRelatedPersonRelationAsync(Expression<Func<ResourceRelatedPersonRelation, bool>> predicate, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;

            return await GetAsync(database.ResourceRelatedPersonRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<ResourceRelatedPersonRelation?> GetResourceRelatedPersonRelationAsync(Expression<Func<ResourceRelatedPersonRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceRelatedPersonRelations, predicate, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<ResourceRelatedPersonRelation[]> GetAllResourceRelatedPersonRelationsAsync(Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;

            return await GetAllAsync(database.ResourceRelatedPersonRelations, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceRelatedPersonRelation[]> GetAllResourceRelatedPersonRelationsAsync(Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedPersonRelations, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceRelatedPersonRelation[]> GetAllResourceRelatedPersonRelationsAsync(Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedPersonRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceRelatedPersonRelation[]> GetAllResourceRelatedPersonRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedPersonRelations, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceRelatedPersonRelation[]> GetResourceRelatedPersonRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceRelatedPersonRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceRelatedPersonRelation[]> GetResourceRelatedPersonRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedPersonRelations, pageIndex, pageSize, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceRelatedPersonRelation[]> GetResourceRelatedPersonRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceRelatedPersonRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceRelatedPersonRelation[]> GetResourceRelatedPersonRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedPersonRelations, pageIndex, pageSize, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllResourceRelatedPersonRelationsAsync(string projection, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceRelatedPersonRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllResourceRelatedPersonRelationsAsync(string projection, Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedPersonRelations, projection, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllResourceRelatedPersonRelationsAsync(string projection, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceRelatedPersonRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllResourceRelatedPersonRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceRelatedPersonRelations, projection, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetResourceRelatedPersonRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceRelatedPersonRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetResourceRelatedPersonRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedPersonRelations, projection, pageIndex, pageSize, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetResourceRelatedPersonRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceRelatedPersonRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetResourceRelatedPersonRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceRelatedPersonRelations, projection, pageIndex, pageSize, resourceRelatedPersonRelationDefaultOrderBy, resourceRelatedPersonRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetResourceRelatedPersonRelationPropertyAsync(Expression<Func<ResourceRelatedPersonRelation, bool>> predicate, string selector, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceRelatedPersonRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetResourceRelatedPersonRelationPropertyOrDefaultAsync(Expression<Func<ResourceRelatedPersonRelation, bool>> predicate, string selector, Expression<Func<ResourceRelatedPersonRelation, object>>? orderBy = null, bool orderDescending = resourceRelatedPersonRelationDefaultOrderDescending)
        {
            orderBy ??= resourceRelatedPersonRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceRelatedPersonRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region ResourceSourceRelation

        // ResourceSourceRelation itself

        private readonly Expression<Func<ResourceSourceRelation, object>> resourceSourceRelationDefaultOrderBy = resourceSourceRelation => resourceSourceRelation.ResourceId;
        private const bool resourceSourceRelationDefaultOrderDescending = true;


        // Single

        public async Task<ResourceSourceRelation?> GetResourceSourceRelationAsync(Expression<Func<ResourceSourceRelation, bool>> predicate, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;

            return await GetAsync(database.ResourceSourceRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<ResourceSourceRelation?> GetResourceSourceRelationAsync(Expression<Func<ResourceSourceRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceSourceRelations, predicate, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<ResourceSourceRelation[]> GetAllResourceSourceRelationsAsync(Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, Expression<Func<ResourceSourceRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;

            return await GetAllAsync(database.ResourceSourceRelations, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceSourceRelation[]> GetAllResourceSourceRelationsAsync(Expression<Func<ResourceSourceRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceSourceRelations, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceSourceRelation[]> GetAllResourceSourceRelationsAsync(Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceSourceRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceSourceRelation[]> GetAllResourceSourceRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceSourceRelations, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceSourceRelation[]> GetResourceSourceRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, Expression<Func<ResourceSourceRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceSourceRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceSourceRelation[]> GetResourceSourceRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceSourceRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceSourceRelations, pageIndex, pageSize, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceSourceRelation[]> GetResourceSourceRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceSourceRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceSourceRelation[]> GetResourceSourceRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceSourceRelations, pageIndex, pageSize, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllResourceSourceRelationsAsync(string projection, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, Expression<Func<ResourceSourceRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceSourceRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllResourceSourceRelationsAsync(string projection, Expression<Func<ResourceSourceRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceSourceRelations, projection, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllResourceSourceRelationsAsync(string projection, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceSourceRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllResourceSourceRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceSourceRelations, projection, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetResourceSourceRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, Expression<Func<ResourceSourceRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceSourceRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetResourceSourceRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceSourceRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceSourceRelations, projection, pageIndex, pageSize, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetResourceSourceRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceSourceRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetResourceSourceRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceSourceRelations, projection, pageIndex, pageSize, resourceSourceRelationDefaultOrderBy, resourceSourceRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetResourceSourceRelationPropertyAsync(Expression<Func<ResourceSourceRelation, bool>> predicate, string selector, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceSourceRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetResourceSourceRelationPropertyOrDefaultAsync(Expression<Func<ResourceSourceRelation, bool>> predicate, string selector, Expression<Func<ResourceSourceRelation, object>>? orderBy = null, bool orderDescending = resourceSourceRelationDefaultOrderDescending)
        {
            orderBy ??= resourceSourceRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceSourceRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region ResourceTagRelation

        // ResourceTagRelation itself

        private readonly Expression<Func<ResourceTagRelation, object>> resourceTagRelationDefaultOrderBy = resourceTagRelation => resourceTagRelation.ResourceId;
        private const bool resourceTagRelationDefaultOrderDescending = true;


        // Single

        public async Task<ResourceTagRelation?> GetResourceTagRelationAsync(Expression<Func<ResourceTagRelation, bool>> predicate, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;

            return await GetAsync(database.ResourceTagRelations, predicate, orderBy, orderDescending, includeProperties);
        }


        public async Task<ResourceTagRelation?> GetResourceTagRelationAsync(Expression<Func<ResourceTagRelation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceTagRelations, predicate, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<ResourceTagRelation[]> GetAllResourceTagRelationsAsync(Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, Expression<Func<ResourceTagRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;

            return await GetAllAsync(database.ResourceTagRelations, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceTagRelation[]> GetAllResourceTagRelationsAsync(Expression<Func<ResourceTagRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTagRelations, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceTagRelation[]> GetAllResourceTagRelationsAsync(Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTagRelations, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceTagRelation[]> GetAllResourceTagRelationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTagRelations, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceTagRelation[]> GetResourceTagRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, Expression<Func<ResourceTagRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceTagRelations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceTagRelation[]> GetResourceTagRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceTagRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTagRelations, pageIndex, pageSize, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceTagRelation[]> GetResourceTagRelationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;

            return await GetPageAsync(database.ResourceTagRelations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceTagRelation[]> GetResourceTagRelationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTagRelations, pageIndex, pageSize, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<dynamic> GetAllResourceTagRelationsAsync(string projection, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, Expression<Func<ResourceTagRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceTagRelations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetAllResourceTagRelationsAsync(string projection, Expression<Func<ResourceTagRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTagRelations, projection, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetAllResourceTagRelationsAsync(string projection, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;
            return await GetAllAsync(database.ResourceTagRelations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetAllResourceTagRelationsAsync(string projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTagRelations, projection, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, null, includeProperties); }

        public async Task<dynamic> GetResourceTagRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, Expression<Func<ResourceTagRelation, bool>>? predicate = null)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceTagRelations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<dynamic> GetResourceTagRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceTagRelation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTagRelations, projection, pageIndex, pageSize, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<dynamic> GetResourceTagRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;
            return await GetPageAsync(database.ResourceTagRelations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<dynamic> GetResourceTagRelationPageAsync(string projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTagRelations, projection, pageIndex, pageSize, resourceTagRelationDefaultOrderBy, resourceTagRelationDefaultOrderDescending, null, includeProperties); }


        // Properties

        public async Task<dynamic?> GetResourceTagRelationPropertyAsync(Expression<Func<ResourceTagRelation, bool>> predicate, string selector, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceTagRelations, predicate, selector, orderBy, orderDescending);
        }

        public async Task<dynamic?> GetResourceTagRelationPropertyOrDefaultAsync(Expression<Func<ResourceTagRelation, bool>> predicate, string selector, Expression<Func<ResourceTagRelation, object>>? orderBy = null, bool orderDescending = resourceTagRelationDefaultOrderDescending)
        {
            orderBy ??= resourceTagRelationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceTagRelations, predicate, selector, orderBy, orderDescending);
        }

        #endregion



    }
}