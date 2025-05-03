"use client";

import React, { useState, useEffect } from "react";
import { Tag } from "@/types/tag.type";
import { FInput, InputBlock } from "@/components/ui/Popup";
import { Button } from "@/components/ui/button";
import AdminTagIcon from "@/icons/tag-icons/admin-tag";
import ApprovedTagIcon from "@/icons/tag-icons/aproved-tag";
import { fetchTagSearch } from "@/actions/tagActions";
import { MAX_TAG_LENGTH } from "@/../constants";

/**
 *
 * @param tags - Tags fetched from root
 * @param className - Styling fetched from parent
 * @param createButton - Whether or not the create button should be added
 *
 * @returns The dropdown box where the user can type and select tags to be added to the document
 */
export default function SelectTagDropdown({ onChangeAction = () => {}, className, standardTag = null}: { onChangeAction?: (selectedTag : string | null) => void, className?: string, standardTag?: Tag | null }) {  

  // States containing the inputvalue, tags returned by the input value, and the tags to be added to the document
  const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
  const [inputValue, setInputValue] = useState<string>("");
  const [addedTag, updateTag] = useState<Tag | null>(standardTag);

  // Update the input value when the user types a character
  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  // Adds new tags to the array
  const addTags = (event: React.MouseEvent<HTMLButtonElement>) => {
    // Cannot add more than 1 tag
    if(addedTag != null){
      return;
    }

    // Get the tag name from the button text.
    const button = event.currentTarget;
    const tagName = button.textContent;

    // Find the corresponding tag object from filteredTags.
    const selectedTag = filteredTags.find((tag) => tag.name === tagName);

    // As long as the selectedTag exists (it should), add to the Tag array and clear input/filtered tags.
    if (selectedTag) {
      updateTag(selectedTag);
      handleTagSelectionChange(selectedTag); // Call the action passed from the parent component
      setInputValue("");
      setFilteredTags([]);
    }
  };

  // Deletes tags from the tag list
  const deleteTags = (event: React.MouseEvent<HTMLButtonElement>) => {
    // Same as above, but here we filter out the tag with the same name
    const button = event.currentTarget;

    if (addedTag != null && addedTag.id == button.name) {
      updateTag(null);
      handleTagSelectionChange(null); // Call the action passed from the parent component
    }
  };

  function handleTagSelectionChange(selectedTag: Tag | null) {
    if(selectedTag == undefined || selectedTag == null){
      onChangeAction(null);
      return;
    }
    onChangeAction(selectedTag.id); // Set the selected tag from the parent component. Since max tags is always 1 we can safely take the first element.
  }

  // Filters tags to display only those tags that correspond with the input value
  async function filterTags() {
    // Ensures we don't add more tags than allowed and we don't render all tags at the start (We want to display filtered tags after at least 1 character is in the input)
    if (inputValue == "" || addedTag != null) {
      setFilteredTags([]);
      return;
    }

    // Gets all values from inserted tags and then filters the tags on uppercase name, sorts them on relevance, and returns top k tags
    const fetchedTags = await fetchTagSearch(inputValue, 5);

    // Don't do anything if fetchedTags returns null or undefined
    if(fetchedTags == null || fetchedTags == undefined || fetchedTags.tags == null || fetchedTags.tags == undefined)
      return;

    // And we update our state
    setFilteredTags(fetchedTags.tags);

  }
  

  useEffect(() => {
    filterTags();
  }, [inputValue]); // Run update when inputValue changes

  useEffect(() => {
    updateTag(standardTag)
  }, [standardTag])

  return (
    <div className={className}>
      <div className="relative w-full">
        <InputBlock className="block w-full">
          <FInput data-testid="input_tags" className="mt-1 w-full" type="string" name="author" placeholder={"Search tags"} value={inputValue} onChange={handleInputChange} maxLength={MAX_TAG_LENGTH} hidden={addedTag != null}/>
        </InputBlock>
        <div className={`absolute right-0 left-0 z-10 max-h-50 max-w-full overflow-y-auto bg-white shadow-lg ${filteredTags.length > 0 ? "rounded border" : ""}`}>
          {filteredTags.map((tag) => (
              <button data-testid="select_tag" key={tag.name} onClick={addTags} type="button" className="flex gap-2 w-full cursor-pointer p-2 text-left transition-colors duration-200 hover:bg-blue-100">
                {tag.name}{ tag.isStandardized ? <AdminTagIcon className="h-4 w-4 self-center" /> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
              </button>
            ))}
        </div>
      </div>
      <div className="mt-1 h-50 max-h-50 w-[20vh] overflow-y-auto border gap-4">
        {addedTag && (
          <div key={addedTag.id} className="flex w-full p-2 text-left transition-colors duration-200 justify-between items-center">
            <div className="flex items-center gap-2">
              <p>{addedTag.name}</p>
              {addedTag.isStandardized ? <AdminTagIcon className="h-4 w-4"/> : addedTag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
            </div>
            <Button data-testid="delete_tag" onClick={deleteTags} name={addedTag.id} className="ml-2 cursor-pointer" type="button">
              Delete
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)