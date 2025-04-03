using KnowledgeBank.Models;

namespace backend.Data
{
    public partial class ResourceManager
    {
        #region Organisation Relationships

        public async Task<bool> RemoveOrganisationRelationshipAsync(Guid sourceOrganisationId, Guid targetOrganisationId)
        {
            OrganisationRelationship? relationship = await database.OrganisationRelationships.FindAsync([sourceOrganisationId, targetOrganisationId]);

            if (relationship == null) return false;

            database.OrganisationRelationships.Remove(relationship);

            return true;
        }

        public async Task<bool> RemoveAllOrganisationRelationshipsWithSourceIdAsync(Guid sourceOrganisationId)
        {
            return false;
        }

        #endregion
    }
}
