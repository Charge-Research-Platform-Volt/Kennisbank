"use client";

import React, { useState, useEffect } from "react";
import { Tag } from "@/types/tag.type";
import { FInput, InputBlock, InputHeader } from "@/components/ui/Popup";
import { Button } from "@/components/ui/button";
import AdminTagIcon from "@/icons/tag-icons/admin-tag";
import ApprovedTagIcon from "@/icons/tag-icons/aproved-tag";
import { AddUserTag } from "@/actions/tagActions";
import { toast } from "sonner";
import { fetchTagSearch } from "@/actions/tagActions";

/**
 *
 * @param tags - Tags fetched from root
 * @param className - Styling fetched from parent
 * @param createButton - Whether or not the create button should be added
 *
 * @returns The dropdown box where the user can type and select tags to be added to the document
 */
export default function TagSelectionDropdown({ onSelectionChangedAction = () => {}, className, createButton = true }: { onSelectionChangedAction?: (tagFilters: string[]) => void, className?: string, createButton?: boolean }) {  
  const MAX_TAGS: number = 10;

  // States containing the inputvalue, tags returned by the input value, and the tags to be added to the document
  const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
  const [inputValue, setInputValue] = useState<string>("");
  const [addedTags, updateTags] = useState<Tag[]>([]);
  const [showCreateTagField, setShowCreateTagField] = useState(false);
  const [newTagName, setNewTagName] = useState("");
  const [isCreatingTag, setIsCreatingTag] = useState(false);

  // Defines placeholder for the tags, user gets a warning when the maximum amount of tags is added
  const tagPlaceholder: string = addedTags.length < MAX_TAGS ? "Search for tags" : "Maximum amount of tags added!";

  // Update the input value when the user types a character
  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  const handleCreateTag = async () => {
    if (!newTagName.trim()) return;
    setIsCreatingTag(true);
    
    // Create a FormData object to send to the server action
    const formData = new FormData();
    formData.append("name", newTagName);
    
    // Call the server action
    const result = await AddUserTag({success: false, message: "", inputs: {name: ""}}, formData);
    if(result.success){
      toast.success(result.message);
    }
    else{
      toast.error(result.message);
    }

    // Reset UI state
    setNewTagName("");
    setShowCreateTagField(false);
    setIsCreatingTag(false);
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
      handleTagSelectionChange(newTags.map(tag => tag.id)); // Call the action passed from the parent component
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
      const updatedTags = addedTags.filter((tag) => tag.name !== selectedTag.name);
      updateTags(updatedTags);
      handleTagSelectionChange(updatedTags.map(tag => tag.id)); // Call the action passed from the parent component
    }
  };

  function handleTagSelectionChange(selectedTags: string[]) {
    onSelectionChangedAction(selectedTags); // Call the action passed from the parent component
  }

  // Filters tags to display only those tags that correspond with the input value
  async function filterTags() {
    // Ensures we don't add more tags than allowed and we don't render all tags at the start (We want to display filtered tags after at least 1 character is in the input)
    if (inputValue == "" || addedTags.length >= MAX_TAGS) {
      setFilteredTags([]);
      return;
    }

    // Gets all values from inserted tags and then filters the tags on uppercase name, sorts them on relevance, and returns top k tags
    const fetchedTags = await fetchTagSearch(inputValue, MAX_TAGS);

    // Don't do anything if fetchedTags returns null or undefined
    if(fetchedTags == null || fetchedTags == undefined)
      return;

    // And we update our state
    setFilteredTags(fetchedTags.filter((tag) => !addedTags.some(addedTag => addedTag.id === tag.id)));
  }
  

  useEffect(() => {
    filterTags();
  }, [inputValue]); // Run update when inputValue changes

  return (
    <div className={className}>
      <div className="relative w-full">
        <InputBlock className="block w-full">
          <div className="flex items-center mt-1">
            <InputHeader className="" data-testid="popup_text">Tags: </InputHeader>
            {createButton ? (
              <div data-testid="popup_text">
                <Button onClick={() => setShowCreateTagField(!showCreateTagField)} className="ml-auto cursor-pointer text-sm" type="button" data-testid="create_cancel_button">
                  {showCreateTagField ? 'Cancel' : 'Create'}
                </Button>
              </div>
            ) : ''}
          </div>

          {showCreateTagField && (
            <div className="mt-1 flex">
              <FInput 
                data-testid="tag_create_name"
                className="flex-grow"
                type="text"
                placeholder="Enter new tag name"
                value={newTagName}
                onChange={(e) => setNewTagName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    handleCreateTag();
                  } else if (e.key === 'Escape') {
                    setShowCreateTagField(false);
                    setNewTagName("");
                  }
                }}
              />
              <Button 
                data-testid="tag_add_button"
                type="button"
                onClick={handleCreateTag}
                disabled={!newTagName.trim() || isCreatingTag}
                className="ml-2 text-sm"
              >
                {isCreatingTag ? 'Adding...' : 'Add'}
              </Button>
            </div>
          )}
          <FInput data-testid="input_tags" className="mt-1 w-full" type="string" name="author" placeholder={tagPlaceholder} value={inputValue} onChange={handleInputChange} />
        </InputBlock>
        <div className={`absolute right-0 left-0 z-10 max-h-50 max-w-full overflow-y-auto bg-white shadow-lg ${filteredTags.length > 0 ? "rounded border" : ""}`}>
          {filteredTags.map((tag) => (
              <button data-testid="select_tag" key={tag.name} onClick={addTags} type="button" className="flex gap-2 w-full cursor-pointer p-2 text-left transition-colors duration-200 hover:bg-blue-100">
                {tag.name}{ tag.isStandardized ? <AdminTagIcon className="h-4 w-4 self-center" /> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
              </button>
            ))}
        </div>
      </div>
      <div className="mt-1 h-50 max-h-50 overflow-y-auto border">
        {addedTags.map((tag) => (
          <div key={tag.name} className="flex w-full p-2 text-left transition-colors duration-200">
            <div className="flex items-center gap-2">
              <p>{tag.name}</p>
              {tag.isStandardized ? <AdminTagIcon className="h-4 w-4"/> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
            </div>
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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


