"use server";

import { ApiResponseSchema } from "@/types/apiResponse.type";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

export const openFile = async (id: string) : Promise<string | undefined> => {
    
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `${process.env.API_URL}/resources/info/${encodeURIComponent(id)}?properties=${encodeURIComponent("FileType")}`,
        {
            method: "GET",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        },
    );

    if (!response.ok) {
        throw new Error(`Problem with getting filetype`);
    }

    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);

    if (data.body.fileType === 'website') {
        const response1 = await fetch(`${process.env.API_URL}/resources/${id}/relations/${encodeURIComponent("website")}`,
            {
                method: "GET",
                credentials: "include",
                headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
            }
        );
    
        if (!response1.ok) {
        throw new Error(`Problem with getting url`);
        }

        const rawData1 = await response1.json();
        const data1 = ApiResponseSchema.parse(rawData1);
        return data1.body.url;
    }

    else {
        const endPoint = `${process.env.API_URL}/resources/download/${id}`;

        try {
            const response2 = await fetch(
                endPoint,
                {
                    method: "GET",
                    credentials: "include",
                    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
                },
            );

            if (!response2.ok) {
            throw new Error(`Problem with getting file`);
            }

            const blob: Blob = await response2.blob();
            const arrayBuffer = await blob.arrayBuffer();
            const base64 = Buffer.from(arrayBuffer).toString('base64');
            return `data:${blob.type};base64,${base64}`;
        
        } catch (error) {
            console.error("Error getting file:", error);
        }
        return undefined;
    }

}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


