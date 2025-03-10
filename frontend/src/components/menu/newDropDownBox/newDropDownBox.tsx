"use client"

import React, { useState, useEffect } from "react";
import { TagsArray, Tag } from "@/types/tag.type";

{/* Tag selection dropdown */}
export default function DropDownBox({tags}:{tags:TagsArray}) {
    const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
    const [inputValue, setInputValue] = useState<string>("");
    const [addedTags, updateTags] = useState<Tag[]>([]);
    
    const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
      setInputValue(event.target.value);
    };

    const addTags = (event: React.MouseEvent<HTMLButtonElement>) => {
        const button = event.currentTarget;
        const tagName = button.textContent; // Get the tag name from the button text.

        if(addedTags.some(elem => elem.name == tagName)){
            setInputValue(""); // Clear input after trying to add already added tag.
            setFilteredTags([]); // clear filtered tags array.
            return
        }

        // Find the corresponding Tag object from filteredTags.
        const selectedTag = filteredTags.find(tag => tag.name === tagName);


        if (selectedTag) {
            updateTags(prevTags => [...prevTags, selectedTag]);
            setInputValue(""); // Clear input after adding the tag.
            setFilteredTags([]); // clear filtered tags after adding tags.
        }
      };

    const deleteTags = (event: React.MouseEvent<HTMLButtonElement>) => {
        const button = event.currentTarget;
        const tagName = button.textContent; // Get the tag name from the button text.

        // Find the corresponding Tag object from filteredTags.
        const selectedTag = addedTags.find(tag => tag.name === tagName);
        console.log(selectedTag);

        if (selectedTag) {
            updateTags(prevTags => prevTags.filter(tag => tag.name !== selectedTag.name));
        } 
        console.log(addedTags);
    }

    function filterTags() {
        const fetchedTags = Object.values(tags);

        if (inputValue == ""){
            setFilteredTags([]);
            return;
          }
        
        const uppercaseInput = inputValue.toUpperCase();
        
        console.log(fetchedTags);

        const filtered = fetchedTags.filter(tag =>
            tag.name.toUpperCase().startsWith(uppercaseInput)
        );

        setFilteredTags(filtered);
    }

    useEffect(() => {
        filterTags();
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
                    className="border rounded p-2 w-full focus:outline-none focus:ring focus:border-blue-300"
                />
                <div className="absolute bg-white border rounded shadow-md mt-1 w-full flex flex-col">
                    {filteredTags.map((tag) => (
                        <button 
                            key={tag.name} 
                            onClick={addTags}
                            className="block w-full text-left p-2 hover:bg-blue-100 transition-colors duration-200">
                                {tag.name}
                        </button>
                    ))}
                </div>
            </div>
            <div>
                {addedTags.map(tag => (
                <button
                    key={tag.name}
                    className="block w-full text-left p-2 hover:bg-blue-100 transition-colors duration-200"
                    onClick={deleteTags}>
                        {tag.name}
                </button>
                ))}
            </div>
        </div>
    );
}