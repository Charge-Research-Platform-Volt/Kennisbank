"use client"

import { toast } from "sonner";

export const handleOpenFile = async (id: string, fileType: string) =>
{
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
    if(fileType != "website"){
        url = `/api/resources/download/${id}`; // download url
        try {
            const link = document.createElement('a'); // create download tag
            link.href = url;
            link.target = '_blank'; // open in new tab

            document.body.appendChild(link);
            link.click(); // open link
            document.body.removeChild(link);

        } catch (error) {
            console.error("Error initiating file download:", error);
            toast.error("Something went wrong with opening the file. Please try again later.")
        }
    }
    // If it is a website, get the url of the archive file and go to that website
    else{
        url = `/api/resources/info/${id}?properties=${encodeURIComponent('WebsiteMetadata.Url')}`;

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
                    window.open(makeValid(jsonresponse.body.url), '_blank') 
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


