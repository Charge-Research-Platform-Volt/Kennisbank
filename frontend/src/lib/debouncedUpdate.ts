import React from "react";

/**
 * Creates a debounced update function for updating fields in the backend.
 * Updates are sent only after a delay (default 500ms) of inactivity.
 *
 * @param timerRef - A ref to store the timeout timer
 * @param apiEndpoint - The API endpoint to send updates to (e.g., "/api/resources/update/123")
 * @param delay - The debounce delay in milliseconds (default: 500)
 * @returns A function that updates a field in the backend with debouncing
 */
export function createDebouncedUpdate(
    timerRef: React.RefObject<NodeJS.Timeout | null> | { current: NodeJS.Timeout | null },
    apiEndpoint: string,
    delay: number = 500
) {
    return (field: string, value: string) => {
        // Clear existing timer
        if (timerRef.current) {
            clearTimeout(timerRef.current);
        }

        // Set new timer to update after delay of inactivity
        timerRef.current = setTimeout(async () => {
            try {
                const response = await fetch(apiEndpoint, {
                    method: "PATCH",
                    headers: { "Content-Type": "application/json" },
                    credentials: "include",
                    body: JSON.stringify({ [field]: value })
                });

                if (!response.ok) {
                    console.error(`Failed to update ${field}`);
                }
            } catch (error) {
                console.error(`Error updating ${field}:`, error);
            }
        }, delay);
    };
}
