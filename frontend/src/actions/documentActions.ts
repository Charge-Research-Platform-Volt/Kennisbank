"use server";

import { FileResourceSchema, type FileBase } from "@/types/resource.type";
import { FormResponse } from "@/types/return.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
//import { Log } from "../../Pino";
import { cookies } from "next/headers";

export const AddDocument = async (
    defaultName: string,
    hash: string,
    prevState: FormResponse<FileBase>,
    formData: FormData,
): Promise<FormResponse<FileBase>> => {
    try {

        // Raw data from the form.
        const rawData: FileBase = {
            title: (formData.get("title") as string)?.trim() || defaultName,
            typeId: "0cc285a8-0f07-11f0-a0a6-5600051f1387", // This is the GUID of the unknown type
            languageCode: "AA",
            description: formData.get("description") as string,
            file: formData.get("file") as File,
            hash: formData.get("hash") as string,
            publicationDate: new Date("1995-12-17T03:24:00")
        };

        // Validate the raw data, if it fails, return an error.
        const validatedData = FileResourceSchema.safeParse(rawData);

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
        
        formData.set("typeId", rawData.typeId);
        formData.set("languageCode", rawData.languageCode);
        formData.set("publicationDate", rawData.publicationDate.toISOString());
        
        formData.set("hash", hash);

        const tagIDs: string[] = [];
        
        for (const [key, value] of formData.entries()) {
            if (key.startsWith('standardizedTags[') && key.endsWith(']')) {
                tagIDs.push(value as string);
            }
        }

        if (tagIDs.length > 0) {
            tagIDs.forEach(tagID => {
                formData.append('tags', tagID);
            });
        }

        const userTagIDs: string[] = [];
        
        for (const [key, value] of formData.entries()) {
            if (key.startsWith('userTags[') && key.endsWith(']')) {
                userTagIDs.push(value as string);
            }
        }

        if (userTagIDs.length > 0) {
            userTagIDs.forEach(tagID => {
                formData.append('tags', tagID);
            });
        }

        // Send the data to the backend.
        const cookieHeader : ReadonlyRequestCookies = await cookies();
        const response : Response = await fetch("http://backend:8080/storage/upload", {
            method: "PUT",
            body: formData,
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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


