"use client"

import React, { useState, useEffect } from "react";
import { TagsArray, Tag } from "@/types/tag.type";

{/* Tag selection dropdown */}
export default function DropDownBox({tags}:{tags:TagsArray}) {
    const MAX_TAGS: number = 10

    // States containing the inputvalue, tags returned by the input value, and the tags to be added to the document
    const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
    const [inputValue, setInputValue] = useState<string>("");
    const [addedTags, updateTags] = useState<Tag[]>([]);
    
    // Display other placeholder text when there the limit of tags is reached
    const inputPlaceholder = (addedTags.length < MAX_TAGS) ? "Type or select tags here" : "No more tags can be added"

    // Update the input value when the user types a character
    const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
      setInputValue(event.target.value);
    };

    // Adds new tags to the array
    const addTags = (event: React.MouseEvent<HTMLButtonElement>) => {
        // Get the tag name from the button text.
        const button = event.currentTarget;
        const tagName = button.textContent;

        // Don't add duplicate tags
        if(addedTags.some(elem => elem.name == tagName)){
            setInputValue(""); // Clear input after trying to add already added tag.
            setFilteredTags([]); // clear filtered tags array.
            return
        }

        // Find the corresponding tag object from filteredTags.
        const selectedTag = filteredTags.find(tag => tag.name === tagName);

        // As long as the selectedTag exists (it should), add to the Tag array and clear input/filtered tags.
        if (selectedTag) {
            const newTags = [...addedTags, selectedTag];
            updateTags(newTags);
            setInputValue("");
            setFilteredTags([]);
        }
      };
    
    // Deletes tags from the tag list
    const deleteTags = (event: React.MouseEvent<HTMLButtonElement>) => {
        // Same as above, but here we filter out the tag with the same name
        const button = event.currentTarget;
        const tagName = button.textContent;

        const selectedTag = addedTags.find(tag => tag.name === tagName);

        if (selectedTag) {
            updateTags(prevTags => prevTags.filter(tag => tag.name !== selectedTag.name));
        }
    }

    // Filters tags to display only those tags that correspond with the input value
    function filterTags() {
        const fetchedTags = Object.values(tags); // Gets all values from inserted tags

        // Ensures we don't add more tags than allowed and we don't render all tags at the start (We want to display filtered tags after at least 1 character is in the input)
        if (inputValue == "" || addedTags.length >= MAX_TAGS){
            setFilteredTags([]);
            return;
          }
        
        // Then we filter the tags on uppercase input/tag.name
        const uppercaseInput = inputValue.toUpperCase();
        
        const filtered = fetchedTags.filter(tag =>
            tag.name.toUpperCase().startsWith(uppercaseInput)
        );

        // And we update our state
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
                    placeholder={inputPlaceholder}
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

            {/* Hidden inputs to send the selected tags to the server */}
            {addedTags.map((tag, index) => (
                <input 
                    key={index}
                    type="hidden" 
                    name={`tags[${index}]`} 
                    value={tag.id} 
                />
            ))}
        </div>
    );
}