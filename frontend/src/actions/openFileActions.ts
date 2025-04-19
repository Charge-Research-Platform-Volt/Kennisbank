import { ResourceResponse, WebsiteResponseSchema } from "@/types/resource.type";

export const handleOpenFile = async (file: ResourceResponse) => {

    let url: string | undefined;

    // If the website is of the form www.input.nl or similar, put https:// before it so it doesn't use the
    // knowledgebank host as its base
    function makeValid(input:string):string {
        console.log(input)
        const validBeginLink = ["https://", "http://"];
        for(const element of validBeginLink) {
            if(input.startsWith(element))
                return input;
        };
        return "https://" + input;
    }

    // Anything but a website at the moment we'll just open from the storage
    if(file.fileType != "website"){
        url = `http://localhost:8080/storage/download/${file.id}`;
        try {
            const response = await fetch(url, {
                method: 'GET',
                credentials: 'include', // Makes sure cookies are included
            });
          
            if (!response.ok) {
                throw new Error(`Error getting file: ${response.statusText}`);
            }

            const blob = await response.blob();
            const blobUrl = URL.createObjectURL(blob);
            
            window.open(blobUrl, '_blank');
        } catch (error) {
            console.error("Error getting file:", error);
        }
    }
    // If it is a website, get the url of the archive file and go to that website
    else{
        const website = WebsiteResponseSchema.safeParse(file);
        if(!website.success)
            throw new Error(`Error fetching website`);
        url = `http://localhost:8080/websiteupload/get-website/${file.id}`;

        try {
            await fetch(url, {
                method: 'GET',
                credentials: 'include', // Makes sure cookies are included
            }).then(response => 
            {
                if(!response.ok)
                    {
                        throw new Error(`Error getting website: ${response.statusText}`)
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


