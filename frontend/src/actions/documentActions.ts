"use server";

import { ResourceBaseSchema, type ResourceBase } from "@/types/resource.type";
import { FormResponse, StorageResponse, StorageResponseSchema } from "@/types/return.type";
import { revalidatePath } from "next/cache";
import { Log } from "../../Pino";
import { cookies } from "next/headers";

export const AddDocument = async (
    defaultName: string,
    hash: string,
    prevState: FormResponse<ResourceBase>,
    formData: FormData,
): Promise<FormResponse<ResourceBase>> => {
    try {
        // Raw data from the form.
        const rawData: ResourceBase = {
            title: (formData.get("title") as string)?.trim() || defaultName,
            typeId: "0cc285a8-0f07-11f0-a0a6-5600051f1387", // This is the GUID of the unknown type
            languageCode: "AA",
            description: formData.get("description") as string,
            file: formData.get("file") as File,
            hash: formData.get("hash") as string,
            publicationDate: new Date("1995-12-17T03:24:00"),
        };
        console.log(rawData);

        // Validate the raw data, if it fails, return an error.
        const validatedData = ResourceBaseSchema.safeParse(rawData);
        console.log(validatedData.success);

        if (!validatedData.success) {
            return {
                success: false,
                message: validatedData.error.errors[0].message,
                inputs: rawData,
            };
        }

        if(rawData.title == defaultName){
            formData.set("title", defaultName);
        }
        
        formData.set("type", rawData.typeId);
        formData.set("languageCode", rawData.languageCode);
        formData.set("publicationDate", rawData.publicationDate.toISOString());
        
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
        const cookieHeader = await cookies();
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
        Log.error(`An error occurred: ${error}`);

        return {
            success: false,
            message: "An error occurred.",
        };
    }
};
