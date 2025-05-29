"use client";

export const TrashResource = async (id: string) => {
    try {
        // Send the data to the backend.
        const response : Response = await fetch(`/api/Resources/trash/${encodeURIComponent(id)}`, {
            method: "PATCH",
            credentials: "include",
        });
        
        const data = await response.json();

        // Check if the request was successful, if not, return an error.
        if (!response.ok) {
            console.log("Something failed");
            console.log(data);
            return {
                success: false,
                message: data.message,
            };
        }

        return {
            success: true,
            message: data.message,
        };
    } catch (error) {
        //Log.error(`An error occurred: ${error}`);
        console.log(error);
        return {
            success: false,
            message: "An error occurred.",
        };
    }
};

export const UntrashResource = async (id: string) => {
    try {
        // Send the data to the backend.
        const response : Response = await fetch(`/api/resources/untrash/${encodeURIComponent(id)}`, {
            method: "PATCH",
            credentials: "include",
        });
        
        const data = await response.json();

        // Check if the request was successful, if not, return an error.
        if (!response.ok) {
            console.log("Something failed");
            console.log(data);
            return {
                success: false,
                message: data.message,
            };
        }

        return {
            success: true,
            message: data.message,
        };
    } catch (error) {
        //Log.error(`An error occurred: ${error}`);
        console.log(error);
        return {
            success: false,
            message: "An error occurred.",
        };
    }
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


