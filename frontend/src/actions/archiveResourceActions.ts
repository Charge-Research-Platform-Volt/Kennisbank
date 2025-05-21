"use server";

import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
//import { Log } from "../../Pino";
import { cookies } from "next/headers";

export const ArchiveResource = async (id: string) => {
    try {
        // Send the data to the backend.
        const cookieHeader : ReadonlyRequestCookies = await cookies();
        const response : Response = await fetch(`${process.env.API_URL}/Resources/archive/${encodeURIComponent(id)}`, {
            method: "PATCH",
            credentials: "include",
            headers: { Cookie: cookieHeader.toString() || "" },
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

        // Revalidate the cache for the archive page.
        revalidatePath("/archive");

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

export const UnarchiveResource = async (id: string) => {
    try {
        // Send the data to the backend.
        const cookieHeader : ReadonlyRequestCookies = await cookies();
        const response : Response = await fetch(`${process.env.API_URL}/Resources/unarchive/${encodeURIComponent(id)}`, {
            method: "PATCH",
            credentials: "include",
            headers: { Cookie: cookieHeader.toString() || "" },
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

        // Revalidate the cache for the archive page.
        revalidatePath("/archive");

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


