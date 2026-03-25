using KnowledgeBank.Data;
using KnowledgeBank.Models;

namespace KnowledgeBank.Utils
{
    public static class RelationUpdateUtil
    {
        /// <summary>
        /// Updates the role/relation field in a relationship between two entities
        /// </summary>
        /// <param name="resourceManager">The resource manager instance</param>
        /// <param name="sourceEntityType">The type of the source entity (resource, person, organisation)</param>
        /// <param name="sourceId">The ID of the source entity</param>
        /// <param name="relationType">The type of relation (organisations, related-persons, related-organisations, related-persons-interpersonal)</param>
        /// <param name="targetId">The ID of the target entity</param>
        /// <param name="newValue">The new role/relation value</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> UpdateRelationRole(
            ResourceManager resourceManager,
            string sourceEntityType,
            string sourceId,
            string relationType,
            string targetId,
            string newValue)
        {
            return (sourceEntityType, relationType) switch
            {
                // Resource -> Organisation
                ("resources", "organisations") =>
                    await resourceManager.UpdateRoleInResourceOrganisationRelationAsync(Guid.Parse(sourceId), Guid.Parse(targetId), newValue),

                // Resource -> Related Person
                ("resources", "related-persons") =>
                    await resourceManager.UpdateRoleInResourceRelatedPersonRelationAsync(Guid.Parse(sourceId), Guid.Parse(targetId), newValue),

                // Person -> Related Resource (reverse: the resource relation seen from person's side)
                ("persons", "related-resources") =>
                    await resourceManager.UpdateRoleInResourceRelatedPersonRelationAsync(Guid.Parse(targetId), Guid.Parse(sourceId), newValue),

                // Person -> Organisation
                ("persons", "organisations") or ("persons", "related-organisations") =>
                    await resourceManager.UpdateRoleInPersonOrganisationRelationAsync(Guid.Parse(sourceId), newValue, Guid.Parse(targetId)),

                // Person -> Person (interpersonal relationships)
                ("persons", "related-persons") =>
                    await resourceManager.UpdateRelationInPersonRelationshipAsync(Guid.Parse(sourceId), newValue, Guid.Parse(targetId)),

                // Organisation -> Related Resource (reverse: the resource relation seen from organisation's side)
                ("organisations", "related-resources") =>
                    await resourceManager.UpdateRoleInResourceOrganisationRelationAsync(Guid.Parse(targetId), Guid.Parse(sourceId), newValue),

                // Organisation -> Person
                ("organisations", "related-persons") =>
                    await resourceManager.UpdateRoleInPersonOrganisationRelationAsync(Guid.Parse(targetId), newValue, Guid.Parse(sourceId)),

                // Organisation -> Organisation (interorganisational relationships)
                ("organisations", "related-organisations") =>
                    await resourceManager.UpdateRelationInOrganisationRelationshipAsync(Guid.Parse(sourceId), newValue, Guid.Parse(targetId)),

                // Default - invalid combination
                _ => false
            };
        }
    }
}
