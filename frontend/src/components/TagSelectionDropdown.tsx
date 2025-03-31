"use client";

import React, { useState, useEffect } from "react";
import { TagsArray, Tag } from "@/types/tag.type";
import { FInput, InputBlock, InputHeader } from "@/components/ui/Popup";
import { Button } from "@/components/ui/button";

/**
 *
 * @param tags - Tags fetched from root
 * @param className - Styling fetched from parent
 *
 * @returns The dropdown box where the user can type and select tags to be added to the document
 */
export default function TagSelectionDropdown({ tags, className }: { tags: TagsArray; className?: string }) {
  const MAX_TAGS: number = 10;

  // States containing the inputvalue, tags returned by the input value, and the tags to be added to the document
  const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
  const [inputValue, setInputValue] = useState<string>("");
  const [addedTags, updateTags] = useState<Tag[]>([]);

  // Defines placeholder for the tags, user gets a warning when the maximum amount of tags is added
  const tagPlaceholder: string = addedTags.length < 10 ? "Search for tags" : "Maximum amount of tags added!";

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
    if (addedTags.some((elem) => elem.name == tagName)) {
      setInputValue(""); // Clear input after trying to add already added tag.
      setFilteredTags([]); // clear filtered tags array.
      return;
    }

    // Find the corresponding tag object from filteredTags.
    const selectedTag = filteredTags.find((tag) => tag.name === tagName);

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
    const tagId: string = button.name;

    const selectedTag = addedTags.find((tag) => tag.id === tagId);

    if (selectedTag) {
      updateTags((prevTags) => prevTags.filter((tag) => tag.name !== selectedTag.name));
    }
  };

  // Filters tags to display only those tags that correspond with the input value
  function filterTags() {
    const fetchedTags = Object.values(tags); // Gets all values from inserted tags

    // Ensures we don't add more tags than allowed and we don't render all tags at the start (We want to display filtered tags after at least 1 character is in the input)
    if (inputValue == "" || addedTags.length >= MAX_TAGS) {
      setFilteredTags([]);
      return;
    }

    // Then we filter the tags on uppercase input/tag.name
    const uppercaseInput: string = inputValue.toUpperCase();

    const filtered = fetchedTags.filter((tag) => tag.name.toUpperCase().startsWith(uppercaseInput));

    // And we update our state
    setFilteredTags(filtered.filter((tag) => !addedTags.includes(tag)));
  }

  useEffect(() => {
    filterTags();
  }, [inputValue]); // Run update when inputValue changes

  return (
    <div className={className}>
      <div className="relative w-full">
        <InputBlock data-testid="popup_text" className="block w-full">
          <InputHeader className="">Tags: </InputHeader>
          <FInput data-testid="input_tags" className="w-full" type="string" name="author" placeholder={tagPlaceholder} onChange={handleInputChange} />
        </InputBlock>
        <div className={`absolute right-0 left-0 z-10 max-h-50 max-w-full overflow-y-auto bg-white shadow-lg ${filteredTags.length > 0 ? "rounded border" : ""}`}>
          {filteredTags.map((tag) => (
            <button data-testid="select_tag" key={tag.name} onClick={addTags} className="block w-full cursor-pointer p-2 text-left transition-colors duration-200 hover:bg-blue-100">
              {tag.name}
            </button>
          ))}
        </div>
      </div>
      <div className="mt-1 h-[100%] max-h-50 overflow-y-auto border">
        {addedTags.map((tag) => (
          <div key={tag.name} className="flex w-full p-2 text-left transition-colors duration-200">
            <p>{tag.name}</p>
            <Button data-testid="delete_tag" onClick={deleteTags} name={tag.id} className="ml-auto cursor-pointer" type="button">
              Delete
            </Button>
          </div>
        ))}
      </div>

      {/* Hidden inputs to send the selected tags to the server */}
      {addedTags.map((tag, index) => (
        <input key={index} type="hidden" name={`tags[${index}]`} value={tag.id} />
      ))}
    </div>
  );
}
