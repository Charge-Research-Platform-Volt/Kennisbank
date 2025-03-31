"use server";

import { DocumentBaseSchema, type DocumentBase } from "@/types/document.type";
import type { FormResponse } from "@/types/return.type";
import { revalidatePath } from "next/cache";
//import { Log } from "../../Pino";
import { cookies } from "next/headers";

export const AddDocument = async (
    defaultName: string,
    hash: string,
    prevState: FormResponse<DocumentBase>,
    formData: FormData,
): Promise<FormResponse<DocumentBase>> => {
    try {
        // Raw data from the form.
        const rawData: DocumentBase = {
            name: (formData.get("name") as string)?.trim() || defaultName,
            description: formData.get("description") as string,
            file: formData.get("file") as File,
            hash: formData.get("hash") as string,
        };

        // Validate the raw data, if it fails, return an error.
        const validatedData = DocumentBaseSchema.safeParse(rawData);

        if (!validatedData.success) {
            return {
                success: false,
                message: validatedData.error.errors[0].message,
                inputs: rawData,
            };
        }

        if(rawData.name == defaultName){
            formData.set("name", defaultName);
        }

        formData.set("hash", hash);

        const tagIDs: string[] = [];
        
        for (const [key, value] of formData.entries()) {
            if (key.startsWith('tags[') && key.endsWith(']')) {
                tagIDs.push(value as string);
            }
        }

        if (tagIDs.length > 0) {
            tagIDs.forEach(tagID => {
                formData.append('tags', tagID);
            });
        }

        // Send the data to the backend.
        const cookieHeader = cookies();
        const response = await fetch("http://backend:8080/storage/upload", {
            method: "PUT",
            body: formData,
            credentials: "include",
            headers: { Cookie: cookieHeader.toString() || "" },
        });
        const data = await response.json();

        // Check if the request was successful, if not, return an error.
        if (!response.ok) {
            return {
                success: false,
                message: data.message,
                inputs: rawData,
            };
        }

        // Revalidate the cache for the home page.
        revalidatePath("/");

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
