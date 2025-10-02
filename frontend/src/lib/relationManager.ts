/**
 * Manages relations for resources by adding or removing them via API.
 */

export type RelationType =
    | "tags"
    | "authors"
    | "organisations"
    | "related-persons"
    | "related-organisations"
    | "sources"
    | "regions";

/**
 * Adds a relation to a resource
 * @param resourceId - The ID of the resource
 * @param relationType - The type of relation to add
 * @param targetId - The ID of the target to relate
 * @returns Promise that resolves to true if successful
 */
export async function addResourceRelation(
    resourceId: string,
    relationType: RelationType,
    targetId: string
): Promise<boolean> {
    try {
        const response = await fetch(`/api/resources/${resourceId}/relations/add/${relationType}/${targetId}`, {
            method: "GET",
            credentials: "include",
        });

        if (!response.ok) {
            console.error(`Failed to add ${relationType} relation`);
            return false;
        }

        return true;
    } catch (error) {
        console.error(`Error adding ${relationType} relation:`, error);
        return false;
    }
}

/**
 * Removes a relation from a resource
 * @param resourceId - The ID of the resource
 * @param relationType - The type of relation to remove
 * @param targetId - The ID of the target to unrelate
 * @returns Promise that resolves to true if successful
 */
export async function removeResourceRelation(
    resourceId: string,
    relationType: RelationType,
    targetId: string
): Promise<boolean> {
    try {
        const response = await fetch(`/api/resources/${resourceId}/relations/remove/${relationType}/${targetId}`, {
            method: "GET",
            credentials: "include",
        });

        if (!response.ok) {
            console.error(`Failed to remove ${relationType} relation`);
            return false;
        }

        return true;
    } catch (error) {
        console.error(`Error removing ${relationType} relation:`, error);
        return false;
    }
}
