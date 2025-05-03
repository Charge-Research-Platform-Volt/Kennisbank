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
 * @param onChangeAction - Which function to call in another component when a value is changed
 * @param className - Optional styling
 * @param standardTag - Default value
 *
 * @returns The dropdown box where the user can type and select a tag to be merged
 */
export default function SelectTagDropdown({ onChangeAction = () => {}, className, standardTag = null}: { onChangeAction?: (selectedTag : string | null) => void, className?: string, standardTag?: Tag | null }) {  

  // States containing the input value, tags returned by the input value, and the tag to be merged
  const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
  const [inputValue, setInputValue] = useState<string>("");
  const [selectedTag, updateTag] = useState<Tag | null>(standardTag);

  // Update the input value when the user types a character
  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  // Change the selected tag
  const addTags = (event: React.MouseEvent<HTMLButtonElement>) => {
    // Cannot add more than 1 tag
    if(selectedTag != null){
      return;
    }

    // Get the tag name from the button text
    const button = event.currentTarget;
    const tagName = button.textContent;

    // Find the corresponding tag object from filteredTags
    const tagToAdd = filteredTags.find((tag) => tag.name === tagName);

    // As long as the selectedTag exists (it should), update the added tag, call the parent function, reset the input value and stop fetching tags
    if (tagToAdd) {
      updateTag(tagToAdd);
      handleTagSelectionChange(tagToAdd); // Call the action passed from the parent component
      setInputValue("");
      setFilteredTags([]);
    }
  };

  // Deletes a selected tag
  const deleteTags = (event: React.MouseEvent<HTMLButtonElement>) => {
    // Check if the tag to be deleted matches the selected tag
    const button = event.currentTarget;

    if (selectedTag != null && selectedTag.id == button.name) {
      updateTag(null);
      handleTagSelectionChange(null); // Call the action passed from the parent component
    }
  };

  function handleTagSelectionChange(SelectedTag: Tag | null) {
    // On deletion set the selected tag to null
    if(SelectedTag == undefined || SelectedTag == null){
      onChangeAction(null);
      return;
    }
    onChangeAction(SelectedTag.id); // Set the selected tag from the parent component
  }

  // Filters tags to display only those tags that correspond with the input value
  async function filterTags() {
    // Ensures we don't add more tags than allowed and we don't render all tags at the start (We want to display filtered tags after at least 1 character is in the input)
    if (inputValue == "" || selectedTag != null) {
      setFilteredTags([]);
      return;
    }

    // Gets all values from inserted tags and then filters the tags on uppercase name, sorts them on relevance, and returns top 5 tags
    const fetchedTags = await fetchTagSearch(inputValue, 5);

    // Don't do anything if fetchedTags returns null or undefined
    if(fetchedTags == null || fetchedTags == undefined || fetchedTags.tags == null || fetchedTags.tags == undefined)
      return;

    // And we update our state
    setFilteredTags(fetchedTags.tags);
  }
  

  useEffect(() => {
    filterTags();
  }, [inputValue]); // Run update on filter when inputValue changes

  useEffect(() => {
    updateTag(standardTag)
  }, [standardTag]) // Run update on the current selectedTag when we reset the standard tag in the parent component

  return (
    <div className={className}>
      <div className="relative w-full">
        <InputBlock className="block w-full">
          <FInput data-testid="input_tags" className="mt-1 w-full" type="string" name="author" placeholder={"Search tags"} value={inputValue} onChange={handleInputChange} maxLength={MAX_TAG_LENGTH} hidden={selectedTag != null}/>
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
        {selectedTag && (
          <div key={selectedTag.id} className="flex w-full p-2 text-left transition-colors duration-200 justify-between items-center">
            <div className="flex items-center gap-2">
              <p>{selectedTag.name}</p>
              {selectedTag.isStandardized ? <AdminTagIcon className="h-4 w-4"/> : selectedTag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
            </div>
            <Button data-testid="delete_tag" onClick={deleteTags} name={selectedTag.id} className="ml-2 cursor-pointer" type="button">
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