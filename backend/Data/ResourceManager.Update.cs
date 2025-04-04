using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net.Sockets;

namespace backend.Data
{
    // This part is for updating properties
    public partial class ResourceManager
    {
        #region Generic functions

        protected async Task<int> UpdatePropertyAsync<T, TProperty>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Func<T, TProperty> propertySelector, TProperty newValue) where T : class
        {
            return await dbSet.Where(predicate).ExecuteUpdateAsync(s => s.SetProperty(propertySelector, _ => newValue));
        }

        #endregion



        // --------------------------------



        #region Resource

        // Title

        public async Task<bool> UpdateResourceTitleAsync(Guid resourceId, string newTitle)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.Title, newTitle) > 0; }

        public async Task<bool> UpdateResourceTitleAsync(string resourceId, string newTitle)
        { return await UpdateResourceTitleAsync(Guid.Parse(resourceId), newTitle); }

        // Description

        public async Task<bool> UpdateResourceDescriptionAsync(Guid resourceId, string? newDescription)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.Description, newDescription) > 0; }

        public async Task<bool> UpdateResourceDescriptionAsync(string resourceId, string? newDescription)
        { return await UpdateResourceDescriptionAsync(Guid.Parse(resourceId), newDescription); }

        // TypeId
        
        public async Task<bool> UpdateResourceTypeAsync(Guid resourceId, Guid newResourceTypeId)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.TypeId, newResourceTypeId) > 0; }

        public async Task<bool> UpdateResourceTypeAsync(Guid resourceId, string newResourceTypeId)
        { return await UpdateResourceTypeAsync(resourceId, Guid.Parse(newResourceTypeId)); }

        public async Task<bool> UpdateResourceTypeAsync(string resourceId, Guid newResourceTypeId)
        { return await UpdateResourceTypeAsync(Guid.Parse(resourceId), newResourceTypeId); }

        public async Task<bool> UpdateResourceTypeAsync(string resourceId, string newResourceTypeId)
        { return await UpdateResourceTypeAsync(Guid.Parse(resourceId), Guid.Parse(newResourceTypeId)); }

        // LanguageCode

        public async Task<bool> UpdateResourceLanguageCodeAsync(Guid resourceId, string newLanguageCode)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.LanguageCode, newLanguageCode) > 0; }

        public async Task<bool> UpdateResourceLanguageCodeAsync(string resourceId, string newLanguageCode)
        { return await UpdateResourceLanguageCodeAsync(Guid.Parse(resourceId), newLanguageCode); }

        // Publication Code

        public async Task<bool> UpdateResourcePublicationCodeAsync(Guid resourceId, string? newPublicationCode)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.PublicationCode, newPublicationCode) > 0; }

        public async Task<bool> UpdateResourcePublicationCodeAsync(string resourceId, string? newPublicationCode)
        { return await UpdateResourcePublicationCodeAsync(Guid.Parse(resourceId), newPublicationCode); }

        // Publication Date

        public async Task<bool> UpdateResourcePublicationDateAsync(Guid resourceId, DateTime newPublicationDate)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.PublicationDate, newPublicationDate) > 0; }

        public async Task<bool> UpdateResourcePublicationDateAsync(string resourceId, DateTime newPublicationDate)
        { return await UpdateResourcePublicationDateAsync(Guid.Parse(resourceId), newPublicationDate); }

        // License

        public async Task<bool> UpdateResourceLicenseAsync(Guid resourceId, string? newLicense)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.License, newLicense) > 0; }

        public async Task<bool> UpdateResourceLicenseAsync(string resourceId, string? newLicense)
        { return await UpdateResourceLicenseAsync(Guid.Parse(resourceId), newLicense); }

        // Note

        public async Task<bool> UpdateResourceNoteAsync(Guid resourceId, string? newNote)
        { return    await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.Note, newNote) > 0; }

        public async Task<bool> UpdateResourceNoteAsync(string resourceId, string? newNote)
        { return await UpdateResourceNoteAsync(Guid.Parse(resourceId), newNote); }

        // Hash

        public async Task<bool> UpdateResourceHashAsync(Guid resourceId, string? newHash)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.Hash, newHash) > 0; }

        public async Task<bool> UpdateResourceHashAsync(string resourceId, string? newHash)
        { return await UpdateResourceHashAsync(Guid.Parse(resourceId), newHash); }

        #endregion

        #region Organisation

        // Name

        public async Task<bool> UpdateOrganisationNameAsync(Guid organisationId, string newName)
        { return await UpdatePropertyAsync(database.Organisations, organisation => organisation.Id == organisationId, organisation => organisation.Name, newName) > 0; }

        public async Task<bool> UpdateOrganisationNameAsync(string organisationId, string newName)
        { return await UpdateOrganisationNameAsync(Guid.Parse(organisationId), newName); }

        // Description

        public async Task<bool> UpdateOrganisationDescriptionAsync(Guid organisationId, string? newDescription)
        { return await UpdatePropertyAsync(database.Organisations, organisation => organisation.Id == organisationId, organisation => organisation.Description, newDescription) > 0; }

        public async Task<bool> UpdateOrganisationDescriptionAsync(string organisationId, string? newDescription)
        { return await UpdateOrganisationDescriptionAsync(Guid.Parse(organisationId), newDescription); }

        // Website

        public async Task<bool> UpdateOrganisationWebsiteAsync(Guid organisationId, string? newWebsite)
        { return await UpdatePropertyAsync(database.Organisations, organisation => organisation.Id == organisationId, organisation => organisation.Website, newWebsite) > 0; }

        public async Task<bool> UpdateOrganisationWebsiteAsync(string organisationId, string? newWebsite)
        { return await UpdateOrganisationWebsiteAsync(Guid.Parse(organisationId), newWebsite); }

        // Email Address

        public async Task<bool> UpdateOrganisationEmailAsync(Guid organisationId, string? newEmail)
        { return await UpdatePropertyAsync(database.Organisations, organisation => organisation.Id == organisationId, organisation => organisation.EmailAddress, newEmail) > 0; }

        public async Task<bool> UpdateOrganisationEmailAsync(string organisationId, string? newEmail)
        { return await UpdateOrganisationEmailAsync(Guid.Parse(organisationId), newEmail); }

        #endregion

        #region Person

        // Name

        public async Task<bool> UpdatePersonNameAsync(Guid personId, string newName)
        { return await UpdatePropertyAsync(database.Persons, person => person.Id == personId, person => person.Name, newName) > 0; }

        public async Task<bool> UpdatePersonNameAsync(string personId, string newName)
        { return await UpdatePersonNameAsync(Guid.Parse(personId), newName); }

        // Occupation

        public async Task<bool> UpdatePersonOccupationAsync(Guid personId, string newOccupation)
        { return await UpdatePropertyAsync(database.Persons, person => person.Id == personId, person => person.Occupation, newOccupation) > 0; }

        public async Task<bool> UpdatePersonOccupationAsync(string personId, string newOccupation)
        { return await UpdatePersonOccupationAsync(Guid.Parse(personId), newOccupation); }

        // Description

        public async Task<bool> UpdatePersonDescriptionAsync(Guid personId, string? newDescription)
        { return await UpdatePropertyAsync(database.Persons, person => person.Id == personId, person => person.Description, newDescription) > 0; }

        public async Task<bool> UpdatePersonDescriptionAsync(string personId, string? newDescription)
        { return await UpdatePersonDescriptionAsync(Guid.Parse(personId), newDescription); }

        // Email Address

        public async Task<bool> UpdatePersonEmailAsync(Guid personId, string? newEmail)
        { return await UpdatePropertyAsync(database.Persons, person => person.Id == personId, person => person.EmailAddress, newEmail) > 0; }

        public async Task<bool> UpdatePersonEmailAsync(string personId, string? newEmail)
        { return await UpdatePersonEmailAsync(Guid.Parse(personId), newEmail); }

        // Linkedin

        public async Task<bool> UpdatePersonLinkedinAsync(Guid personId, string? newLinkedin)
        { return await UpdatePropertyAsync(database.Persons, person => person.Id == personId, person => person.Linkedin, newLinkedin) > 0; }

        public async Task<bool> UpdatePersonLinkedinAsync(string personId, string? newLinkedin)
        { return await UpdatePersonLinkedinAsync(Guid.Parse(personId), newLinkedin); }

        #endregion

        #region Region

        // Name

        public async Task<bool> UpdateRegionNameAsync(Guid regionId, string newName)
        { return await UpdatePropertyAsync(database.Regions, region => region.Id == regionId, region => region.Name, newName) > 0; }

        public async Task<bool> UpdateRegionNameAsync(string regionId, string newName)
        { return await UpdateRegionNameAsync(Guid.Parse(regionId), newName); }

        #endregion

        #region AudioMetadata

        // Length

        public async Task<bool> UpdateAudioLengthAsync(Guid resourceId, ulong? newLength)
        { return await UpdatePropertyAsync(database.AudioMetadata, metadata => metadata.ResourceId == resourceId, metadata => metadata.Length, newLength) > 0; }

        public async Task<bool> UpdateAudioLengthAsync(string resourceId, ulong? newLength)
        { return await UpdateAudioLengthAsync(Guid.Parse(resourceId), newLength); }

        #endregion

        #region VideoMetadata

        // Length

        public async Task<bool> UpdateVideoLengthAsync(Guid resourceId, ulong? newLength)
        { return await UpdatePropertyAsync(database.VideoMetadata, metadata => metadata.ResourceId == resourceId, metadata => metadata.Length, newLength) > 0; }

        public async Task<bool> UpdateVideoLengthAsync(string resourceId, ulong? newLength)
        { return await UpdateVideoLengthAsync(Guid.Parse(resourceId), newLength); }

        #endregion

        #region DocumentMetadata

        // Abstract

        public async Task<bool> UpdateDocumentAbstractAsync(Guid resourceId, string? newAbstract)
        { return await UpdatePropertyAsync(database.DocumentMetadata, metadata => metadata.ResourceId == resourceId, metadata => metadata.Abstract, newAbstract) > 0; }

        public async Task<bool> UpdateDocumentAbstractAsync(string resourceId, string? newAbstract)
        { return await UpdateDocumentAbstractAsync(Guid.Parse(resourceId), newAbstract); }

        #endregion

        #region WebsiteMetadata

        // Accessed On

        public async Task<bool> UpdateWebsiteAccessDateAsync(Guid resourceId, DateTime? newAccessDate)
        { return await UpdatePropertyAsync(database.WebsiteMetadata, metadata => metadata.ResourceId == resourceId, metadata => metadata.AccessedOn, newAccessDate) > 0; }

        public async Task<bool> UpdateWebsiteAccessDateAsync(string resourceId, DateTime? newAccessDate)
        { return await UpdateWebsiteAccessDateAsync(Guid.Parse(resourceId), newAccessDate); }

        #endregion

        #region User Tag

        // Name

        public async Task<bool> UpdateUserTagNameAsync(Guid tagId, string newName)
        { return await UpdatePropertyAsync(database.UserTags, tag => tag.Id == tagId, tag => tag.Name, newName) > 0; }

        public async Task<bool> UpdateUserTagNameAsync(string tagId, string newName)
        { return await UpdateUserTagNameAsync(Guid.Parse(tagId), newName); }

        // Approve

        public async Task<bool> ApproveUserTagAsync(Guid tagId, Guid adminId)
        {
                    await UpdatePropertyAsync(database.UserTags, tag => tag.Id == tagId, tag => tag.IsApproved, true);
                    await UpdatePropertyAsync(database.UserTags, tag => tag.Id == tagId, tag => tag.ApprovedBy, adminId);
            return  await UpdatePropertyAsync(database.UserTags, tag => tag.Id == tagId, tag => tag.ApprovedOn, DateTime.UtcNow) > 0;
        }

        public async Task<bool> ApproveUserTagAsync(Guid tagId, string adminId)
        { return await ApproveUserTagAsync(tagId, Guid.Parse(adminId)); }

        public async Task<bool> ApproveUserTagAsync(string tagId, Guid adminId)
        { return await ApproveUserTagAsync(Guid.Parse(tagId), adminId); }

        public async Task<bool> ApproveUserTagAsync(string tagId, string adminId)
        { return await ApproveUserTagAsync(Guid.Parse(tagId), Guid.Parse(adminId)); }

        // Unapprove

        public async Task<bool> UnapproveUserTagAsync(Guid tagId)
        {
                    await UpdatePropertyAsync(database.UserTags, tag => tag.Id == tagId, tag => tag.IsApproved, false);
                    await UpdatePropertyAsync(database.UserTags, tag => tag.Id == tagId, tag => tag.ApprovedBy, null);
            return  await UpdatePropertyAsync(database.UserTags, tag => tag.Id == tagId, tag => tag.ApprovedOn, null) > 0;
        }

        public async Task<bool> UnapproveUserTagAsync(string tagId)
        { return await UnapproveUserTagAsync(Guid.Parse(tagId)); }

        #endregion

        #region Admin Tag

        // Name

        public async Task<bool> UpdateAdminTagNameAsync(Guid tagId, string newName)
        { return await UpdatePropertyAsync(database.AdminTags, tag => tag.Id == tagId, tag => tag.Name, newName) > 0; }

        public async Task<bool> UpdateAdminTagNameAsync(string tagId, string newName)
        { return await UpdateAdminTagNameAsync(Guid.Parse(tagId), newName); }

        #endregion

        #region Resource Type

        // Name

        public async Task<bool> UpdateResourceTypeNameAsync(Guid resourceTypeId, string newName)
        { return await UpdatePropertyAsync(database.ResourceTypes, type => type.Id == resourceTypeId, type => type.Name, newName) > 0; }

        public async Task<bool> UpdateResourceTypeNameAsync(string resourceTypeId, string newName)
        { return await UpdateResourceTypeNameAsync(Guid.Parse(resourceTypeId), newName); }

        #endregion

        #region Organisation Relationship

        // Relation

        public async Task<bool> UpdateRelationInOrganisationRelationshipAsync(Guid sourceOrganisationId, string newRelation, Guid targetOrganisationId)
        { return await UpdatePropertyAsync(database.OrganisationRelationships, relation => relation.SourceOrganisationId == sourceOrganisationId && relation.TargetOrganisationId == targetOrganisationId, relationship => relationship.Relation, newRelation) > 0; }

        public async Task<bool> UpdateRelationInOrganisationRelationshipAsync(Guid sourceOrganisationId, string newRelation, string targetOrganisationId)
        { return await UpdateRelationInOrganisationRelationshipAsync(sourceOrganisationId, newRelation, Guid.Parse(targetOrganisationId)); }

        public async Task<bool> UpdateRelationInOrganisationRelationshipAsync(string sourceOrganisationId, string newRelation, Guid targetOrganisationId)
        { return await UpdateRelationInOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), newRelation, targetOrganisationId); }

        public async Task<bool> UpdateRelationInOrganisationRelationshipAsync(string sourceOrganisationId, string newRelation, string targetOrganisationId)
        { return await UpdateRelationInOrganisationRelationshipAsync(Guid.Parse(sourceOrganisationId), newRelation, Guid.Parse(targetOrganisationId)); }

        #endregion

        #region Person Organisation

        // Role

        public async Task<bool> UpdateRoleInPersonOrganisatinoRelationAsync(Guid personId, string newRole, Guid organisationId)
        { return await UpdatePropertyAsync(database.PersonOrganisationRelations, relation => relation.PersonId == personId && relation.OrganisationId == organisationId, relation => relation.Role, newRole) > 0; }

        public async Task<bool> UpdateRoleInPersonOrganisatinoRelationAsync(Guid personId, string newRole, string organisationId)
        { return await UpdateRoleInPersonOrganisatinoRelationAsync(personId, newRole, Guid.Parse(organisationId)); }
        public async Task<bool> UpdateRoleInPersonOrganisatinoRelationAsync(string personId, string newRole, Guid organisationId)
        { return await UpdateRoleInPersonOrganisatinoRelationAsync(Guid.Parse(personId), newRole, organisationId); }

        public async Task<bool> UpdateRoleInPersonOrganisatinoRelationAsync(string personId, string newRole, string organisationId)
        { return await UpdateRoleInPersonOrganisatinoRelationAsync(Guid.Parse(personId), newRole, Guid.Parse(organisationId)); }

        #endregion


        #region Person Relationship

        // Relation

        public async Task<bool> UpdateRelationInPersonRelationshipAsync(Guid sourcePersonId, string newRelation, Guid targetPersonId)
        { return await UpdatePropertyAsync(database.PersonRelationships, relation => relation.SourcePersonId == sourcePersonId && relation.TargetPersonId == targetPersonId, relationship => relationship.Relation, newRelation) > 0; }

        public async Task<bool> UpdateRelationInPersonRelationshipAsync(Guid sourcePersonId, string newRelation, string targetPersonId)
        { return await UpdateRelationInPersonRelationshipAsync(sourcePersonId, newRelation, Guid.Parse(targetPersonId)); }

        public async Task<bool> UpdateRelationInPersonRelationshipAsync(string sourcePersonId, string newRelation, Guid targetPersonId)
        { return await UpdateRelationInPersonRelationshipAsync(Guid.Parse(sourcePersonId), newRelation, targetPersonId); }

        public async Task<bool> UpdateRelationInPersonRelationshipAsync(string sourcePersonId, string newRelation, string targetPersonId)
        { return await UpdateRelationInPersonRelationshipAsync(Guid.Parse(sourcePersonId), newRelation, Guid.Parse(targetPersonId)); }

        #endregion

        #region Resource Organisation

        // Role

        public async Task<bool> UpdateRoleInResourceOrganisationRelationAsync(Guid resourceId, Guid organisationId, string newRole)
        { return await UpdatePropertyAsync(database.ResourceOrganisationRelations, relation => relation.ResourceId == resourceId && relation.OrganisationId == organisationId, relation => relation.Role, newRole) > 0; }

        public async Task<bool> UpdateRoleInResourceOrganisationRelationAsync(Guid resourceId, string organisationId, string newRole)
        { return await UpdateRoleInResourceOrganisationRelationAsync(resourceId, Guid.Parse(organisationId), newRole); }

        public async Task<bool> UpdateRoleInResourceOrganisationRelationAsync(string resourceId, Guid organisationId, string newRole)
        { return await UpdateRoleInResourceOrganisationRelationAsync(Guid.Parse(resourceId), organisationId, newRole); }

        public async Task<bool> UpdateRoleInResourceOrganisationRelationAsync(string resourceId, string organisationId, string newRole)
        { return await UpdateRoleInResourceOrganisationRelationAsync(Guid.Parse(resourceId), Guid.Parse(organisationId), newRole); }

        #endregion

        #region Resource Related Organisation

        // Role

        public async Task<bool> UpdateRoleInResourceRelatedOrganisationRelationAsync(Guid resourceId, Guid organisationId, string newRole)
        { return await UpdatePropertyAsync(database.ResourceRelatedOrganisationRelations, relation => relation.ResourceId == resourceId && relation.OrganisationId == organisationId, relation => relation.Role, newRole) > 0; }

        public async Task<bool> UpdateRoleInResourceRelatedOrganisationRelationAsync(Guid resourceId, string organisationId, string newRole)
        { return await UpdateRoleInResourceRelatedOrganisationRelationAsync(resourceId, Guid.Parse(organisationId), newRole); }

        public async Task<bool> UpdateRoleInResourceRelatedOrganisationRelationAsync(string resourceId, Guid organisationId, string newRole)
        { return await UpdateRoleInResourceRelatedOrganisationRelationAsync(Guid.Parse(resourceId), organisationId, newRole); }

        public async Task<bool> UpdateRoleInResourceRelatedOrganisationRelationAsync(string resourceId, string organisationId, string newRole)
        { return await UpdateRoleInResourceRelatedOrganisationRelationAsync(Guid.Parse(resourceId), Guid.Parse(organisationId), newRole); }

        #endregion

        #region Resource Related Person

        // Role

        public async Task<bool> UpdateRoleInResourceRelatedPersonRelationAsync(Guid resourceId, Guid personId, string newRole)
        { return await UpdatePropertyAsync(database.ResourceRelatedPersonRelations, relation => relation.ResourceId == resourceId && relation.PersonId == personId, relation => relation.Role, newRole) > 0; }

        public async Task<bool> UpdateRoleInResourceRelatedPersonRelationAsync(Guid resourceId, string personId, string newRole)
        { return await UpdateRoleInResourceRelatedPersonRelationAsync(resourceId, Guid.Parse(personId), newRole); }

        public async Task<bool> UpdateRoleInResourceRelatedPersonRelationAsync(string resourceId, Guid personId, string newRole)
        { return await UpdateRoleInResourceRelatedPersonRelationAsync(Guid.Parse(resourceId), personId, newRole); }

        public async Task<bool> UpdateRoleInResourceRelatedPersonRelationAsync(string resourceId, string personId, string newRole)
        { return await UpdateRoleInResourceRelatedPersonRelationAsync(Guid.Parse(resourceId), Guid.Parse(personId), newRole); }

        #endregion

        #region Resource User Tag

        // Approve

        public async Task<bool> ApproveUserTagOnResourceAsync(Guid tagId, Guid resourceId, Guid adminId)
        {
                    await UpdatePropertyAsync(database.ResourceUserTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId, tag => tag.IsApproved, true);
                    await UpdatePropertyAsync(database.ResourceUserTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId, tag => tag.ApprovedBy, adminId);
            return  await UpdatePropertyAsync(database.ResourceUserTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId, tag => tag.ApprovedOn, DateTime.UtcNow) > 0;
        }

        public async Task<bool> ApproveUserTagOnResourceAsync(Guid tagId, Guid resourceId, string adminId)
        { return await ApproveUserTagOnResourceAsync(tagId, resourceId, Guid.Parse(adminId)); }

        public async Task<bool> ApproveUserTagOnResourceAsync(Guid tagId, string resourceId, Guid adminId)
        { return await ApproveUserTagOnResourceAsync(tagId, Guid.Parse(resourceId), adminId); }

        public async Task<bool> ApproveUserTagOnResourceAsync(string tagId, Guid resourceId, Guid adminId)
        { return await ApproveUserTagOnResourceAsync(Guid.Parse(tagId), resourceId, adminId); }

        public async Task<bool> ApproveUserTagOnResourceAsync(string tagId, string resourceId, Guid adminId)
        { return await ApproveUserTagOnResourceAsync(Guid.Parse(tagId), Guid.Parse(resourceId), adminId); }

        public async Task<bool> ApproveUserTagOnResourceAsync(Guid tagId, string resourceId, string adminId)
        { return await ApproveUserTagOnResourceAsync(tagId, Guid.Parse(resourceId), Guid.Parse(adminId)); }

        public async Task<bool> ApproveUserTagOnResourceAsync(string tagId, Guid resourceId, string adminId)
        { return await ApproveUserTagOnResourceAsync(Guid.Parse(tagId), resourceId, Guid.Parse(adminId)); }

        public async Task<bool> ApproveUserTagOnResourceAsync(string tagId, string resourceId, string adminId)
        { return await ApproveUserTagOnResourceAsync(Guid.Parse(tagId), Guid.Parse(resourceId), Guid.Parse(adminId)); }

        // Unapprove

        public async Task<bool> UnapproveUserTagOnResourceAsync(Guid tagId, Guid resourceId, Guid adminId)
        {
                    await UpdatePropertyAsync(database.ResourceUserTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId, tag => tag.IsApproved, false);
                    await UpdatePropertyAsync(database.ResourceUserTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId, tag => tag.ApprovedBy, null);
            return  await UpdatePropertyAsync(database.ResourceUserTagRelations, relation => relation.ResourceId == resourceId && relation.TagId == tagId, tag => tag.ApprovedOn, null) > 0;
        }

        public async Task<bool> UnapproveUserTagOnResourceAsync(Guid tagId, Guid resourceId, string adminId)
        { return await UnapproveUserTagOnResourceAsync(tagId, resourceId, Guid.Parse(adminId)); }

        public async Task<bool> UnapproveUserTagOnResourceAsync(Guid tagId, string resourceId, Guid adminId)
        { return await UnapproveUserTagOnResourceAsync(tagId, Guid.Parse(resourceId), adminId); }

        public async Task<bool> UnapproveUserTagOnResourceAsync(string tagId, Guid resourceId, Guid adminId)
        { return await UnapproveUserTagOnResourceAsync(Guid.Parse(tagId), resourceId, adminId); }

        public async Task<bool> UnapproveUserTagOnResourceAsync(string tagId, string resourceId, Guid adminId)
        { return await UnapproveUserTagOnResourceAsync(Guid.Parse(tagId), Guid.Parse(resourceId), adminId); }

        public async Task<bool> UnapproveUserTagOnResourceAsync(Guid tagId, string resourceId, string adminId)
        { return await UnapproveUserTagOnResourceAsync(tagId, Guid.Parse(resourceId), Guid.Parse(adminId)); }

        public async Task<bool> UnapproveUserTagOnResourceAsync(string tagId, Guid resourceId, string adminId)
        { return await UnapproveUserTagOnResourceAsync(Guid.Parse(tagId), resourceId, Guid.Parse(adminId)); }

        public async Task<bool> UnapproveUserTagOnResourceAsync(string tagId, string resourceId, string adminId)
        { return await UnapproveUserTagOnResourceAsync(Guid.Parse(tagId), Guid.Parse(resourceId), Guid.Parse(adminId)); }

        #endregion
    }
}
