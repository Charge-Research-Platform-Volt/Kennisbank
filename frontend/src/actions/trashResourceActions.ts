"use client";

import { MetadataTypeEnum } from "@/context/left-sidebar-provider";
import { toast } from "sonner";

export const TrashResource = async (id: string, type: MetadataTypeEnum) =>
{
    try
    {
        // Send the data to the backend.
        const response : Response = await fetch(`/api/${type}s/trash/${encodeURIComponent(id)}`,
        {
            method: "PATCH",
            credentials: "include",
        });
        
        const data = await response.json();

        // Check if the request was successful, if not, return an error.
        if (!response.ok)
        {
            console.log("Something failed");
            console.log(data);
            toast.error("Error while deleting, try again.");
            return false;
        }
        
        toast.success("Successfully deleted");
        
        return true;
    }
    catch (error) 
    {
        console.log(error);
        return false;
    }
};

export const UntrashResource = async (id: string, type: MetadataTypeEnum) => {
    try 
    {
        // Send the data to the backend.
        const response : Response = await fetch(`/api/${type}s/untrash/${encodeURIComponent(id)}`,
        {
            method: "PATCH",
            credentials: "include",
        });
        
        const data = await response.json();

        // Check if the request was successful, if not, return an error.
        if (!response.ok)
        {
            console.log("Something failed");
            console.log(data);
            toast.error("Error while restoring, try again.");
            return false;
        }

        toast.success("Successfully restored");
        return true;
    }
    catch (error)
    {
        console.log(error);
        return false;
    }
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


