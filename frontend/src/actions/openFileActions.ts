"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { ResourceResponse, WebsiteResponseSchema } from "@/types/resource.type";
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
    console.log(response);

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
        let endPoint = `${process.env.API_URL}/resources/download/${id}`;

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

export const handleOpenFile = async (file: ResourceResponse) => {

    let url: string | undefined;

    // If the website is of the form www.input.nl or similar, put https:// before it so it doesn't use the
    // knowledgebank host as its base
    function makeValid(input:string):string {
        const validBeginLink : [string, string] = ["https://", "http://"];
        for(const element of validBeginLink) {
            if(input.startsWith(element))
                return input;
        };
        return "https://" + input;
    }

    // Anything but a website at the moment we'll just open from the storage
    if(file.fileType != "website"){
        url = `/api/resources/download/${file.id}`;
        try {
            const response : Response = await fetch(url, {
                method: 'GET',
                credentials: 'include', // Makes sure cookies are included
            });
          
            if (!response.ok) {
                console.error(`Error getting file: ${response.statusText}`);
                return;
            }

            const blob : Blob = await response.blob();
            const blobUrl : string = URL.createObjectURL(blob);
            
            window.open(blobUrl, '_blank');
        } catch (error) {
            console.error("Error getting file:", error);
        }
    }
    // If it is a website, get the url of the archive file and go to that website
    else{
        const website = WebsiteResponseSchema.safeParse(file);
        if(!website.success){
            console.error(`Error fetching website`);
            return;
        }
        url = `/api/websiteupload/get-website/${file.id}`;

        try {
            await fetch(url, {
                method: 'GET',
                credentials: 'include', // Makes sure cookies are included
            }).then(response => 
            {
                if(!response.ok)
                    {
                        console.error(`Error getting website: ${response.statusText}`);
                        return;
                    }
                else {
                    return response.json()
                }
                // if all succeeds open the window, making it valid (otherwise it will direct to our domain + url)
            }).then(jsonresponse =>
                {
                    window.open(makeValid(jsonresponse), '_blank') 
                })
        // other errors
        } catch (error) {
            console.error("Error getting website:", error);
        }
    }
  }

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


