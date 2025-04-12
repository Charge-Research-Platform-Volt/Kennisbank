import { ResourceResponse, WebsiteResponseSchema } from "@/types/resource.type";

export const handleOpenFile = async (file: ResourceResponse) => {

    let url: string | undefined;

    function makeValid(input:string):string {
        const validBeginLink = ["https://", "http://"];
        validBeginLink.forEach(element => {
            if(input.startsWith(element))
                return input;
        });
        return "https://" + input;
    }

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
                    window.open(makeValid(jsonresponse['url']), '_blank') 
                })
        // other errors
        } catch (error) {
            console.error("Error getting website:", error);
        }
    }
  }