/**
 * Manages relations for resources, persons, and organisations by adding or removing them via API.
 */

export type EntityType = "resources" | "persons" | "organisations";

export type RelationType =
    | "tags"
    | "authors"
    | "organisations"
    | "related-persons"
    | "related-organisations"
    | "sources"
    | "regions"
    | "authored-resources"
    | "related-resources"
    | "person-related-persons";

/**
 * Adds a relation to an entity (resource, person, or organisation)
 * @param entityType - The type of entity (resources, persons, organisations)
 * @param entityId - The ID of the entity
 * @param relationType - The type of relation to add
 * @param targetId - The ID of the target to relate
 * @returns Promise that resolves to true if successful
 */
export async function addRelation(
    entityType: EntityType,
    entityId: string,
    relationType: RelationType,
    targetId: string
): Promise<boolean> {
    try {
        const response = await fetch(`/api/${entityType}/${entityId}/relations/add/${relationType}/${targetId}`, {
            method: "GET",
            credentials: "include",
        });

        if (!response.ok) {
            console.error(`Failed to add ${relationType} relation to ${entityType}`);
            return false;
        }

        return true;
    } catch (error) {
        console.error(`Error adding ${relationType} relation to ${entityType}:`, error);
        return false;
    }
}

/**
 * Removes a relation from an entity (resource, person, or organisation)
 * @param entityType - The type of entity (resources, persons, organisations)
 * @param entityId - The ID of the entity
 * @param relationType - The type of relation to remove
 * @param targetId - The ID of the target to unrelate
 * @returns Promise that resolves to true if successful
 */
export async function removeRelation(
    entityType: EntityType,
    entityId: string,
    relationType: RelationType,
    targetId: string
): Promise<boolean> {
    try {
        const response = await fetch(`/api/${entityType}/${entityId}/relations/remove/${relationType}/${targetId}`, {
            method: "GET",
            credentials: "include",
        });

        if (!response.ok) {
            console.error(`Failed to remove ${relationType} relation from ${entityType}`);
            return false;
        }

        return true;
    } catch (error) {
        console.error(`Error removing ${relationType} relation from ${entityType}:`, error);
        return false;
    }
}
