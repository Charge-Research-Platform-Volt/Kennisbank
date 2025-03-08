import React, { useState, useEffect } from "react";
import { z } from "zod";
//import { FetchWithValidation } from "@/lib/fetchWithValidation";

{/* Tag selection dropdown */}
const TagsArraySchema = z.array(z.object({ name: z.string() }));
type Tag = z.infer<typeof TagsArraySchema>[number];

export default function DropDownBox() {
    const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
    const [inputValue, setInputValue] = useState<string>("");

    /*const fetchAndFilterTags = async () => {
        try {
            const result = await FetchWithValidation(
                TagsArraySchema,
                "http://localhost:8080/Tag/all-tags"
            );

            if (!result.success) {
                console.error("Data validation failed:", result.error);
                return;
            }

            const uppercaseInput = inputValue.toUpperCase();

            const filtered = result.data.filter((tag) =>
                tag.name.toUpperCase().startsWith(uppercaseInput)
            );

            setFilteredTags(filtered);
        } catch (error) {
            console.error("Error processing tags:", error);
        }
    };*/

    
    function update() { 
      if (inputValue == ""){
        setFilteredTags([]);
        return;
      }
      const fetch = [{name: "test"}, {name: "ape"}, {name: "what"}]; //test code

      const uppercaseInput = inputValue.toUpperCase();

      const filtered = fetch.filter((tag) =>
          tag.name.toUpperCase().startsWith(uppercaseInput)
      );
      
      setFilteredTags(filtered); 
    }

    const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
      setInputValue(event.target.value);
    };

    useEffect(() => {
        update();
    }, [inputValue]); // Run update when inputValue changes

    return (
        <div>
            <h2 className="pt-2">Add tags:</h2>
            <div className="relative inline-block ">
                <input
                    placeholder="Type or select tags here"
                    id="input"
                    value={inputValue}
                    onChange={handleInputChange}
                />
                <div className="absolute bg-white border rounded shadow-md mt-1 w-full">
                    {filteredTags.map((tag) => (
                        <button key={tag.name}>{tag.name}</button>
                    ))}
                </div>
            </div>
        </div>
    );
}