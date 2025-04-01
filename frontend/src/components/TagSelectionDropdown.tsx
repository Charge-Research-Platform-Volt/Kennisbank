"use client";

import React, { useState, useEffect } from "react";
import { TagsArray, Tag, UserTagsArray, UserTag } from "@/types/tag.type";
import { FInput, InputBlock, InputHeader } from "@/components/ui/Popup";
import { Button } from "@/components/ui/button";
import AdminTag from "@/icons/tag-icons/admin-tag";
import ApprovedTag from "@/icons/tag-icons/aproved-tag";
import { AddUserTag } from "@/actions/userTagActions";
import { toast } from "sonner";

/**
 *
 * @param tags - Tags fetched from root
 * @param className - Styling fetched from parent
 *
 * @returns The dropdown box where the user can type and select tags to be added to the document
 */
export default function TagSelectionDropdown({ userTags, standardizedTags, className }: { userTags: UserTagsArray, standardizedTags: TagsArray; className?: string }) {  
  const MAX_TAGS: number = 10;

  // States containing the inputvalue, tags returned by the input value, and the tags to be added to the document
  const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
  const [inputValue, setInputValue] = useState<string>("");
  const [addedTags, updateTags] = useState<Tag[]>([]);
  const [filteredUserTags, setFilteredUserTags] = useState<UserTag[]>([]);
  const [addedUserTags, updateUserTags] = useState<UserTag[]>([]);
  const [showCreateTagField, setShowCreateTagField] = useState(false);
  const [newTagName, setNewTagName] = useState("");
  const [isCreatingTag, setIsCreatingTag] = useState(false);

  // Defines placeholder for the tags, user gets a warning when the maximum amount of tags is added
  const tagPlaceholder: string = addedTags.length + addedUserTags.length < MAX_TAGS ? "Search for tags" : "Maximum amount of tags added!";

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
      setInputValue("");
      setFilteredTags([]);
    }
  };

  // Adds new tags to the array
  const addUserTags = (event: React.MouseEvent<HTMLButtonElement>) => {
    console.log("Adding user tag");

    // Get the tag name from the button text.
    const button = event.currentTarget;
    const tagName = button.textContent;

    // Don't add duplicate tags
    if (addedTags.some((elem) => elem.name == tagName)) {
      setInputValue(""); // Clear input after trying to add already added tag.
      setFilteredUserTags([]); // clear filtered tags array.
      return;
    }

    // Find the corresponding tag object from filteredTags.
    const selectedUserTag = filteredUserTags.find((tag) => tag.name === tagName);

    // As long as the selectedTag exists (it should), add to the Tag array and clear input/filtered tags.
    if (selectedUserTag) {
      const newUserTags = [...addedUserTags, selectedUserTag];
      updateUserTags(newUserTags);
      setInputValue("");
      setFilteredUserTags([]);
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

  // Deletes usertags from the usertag list
  const deleteUserTags = (event: React.MouseEvent<HTMLButtonElement>) => {
    // Same as above, but here we filter out the tag with the same name
    const button = event.currentTarget;
    const tagId: string = button.name;

    const selectedUserTag = addedUserTags.find((tag) => tag.id === tagId);

    if (selectedUserTag) {
      updateUserTags((prevUserTags) => prevUserTags.filter((tag) => tag.name !== selectedUserTag.name));
    }
  };

  // Filters tags to display only those tags that correspond with the input value
  function filterTags() {
    const fetchedTags = Object.values(standardizedTags); // Gets all values from inserted tags

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

  // Filters user tags to display only those user tags that correspond with the input value
  function filterUserTags() {
    const fetchedUserTags = Object.values(userTags); // Gets all values from inserted tags

    // Ensures we don't add more tags than allowed and we don't render all tags at the start (We want to display filtered tags after at least 1 character is in the input)
    if (inputValue == "" || addedTags.length >= MAX_TAGS) {
      setFilteredUserTags([]);
      return;
    }

    // Then we filter the tags on uppercase input/tag.name
    const uppercaseInput: string = inputValue.toUpperCase();

    const filtered = fetchedUserTags.filter((tag) => tag.name.toUpperCase().startsWith(uppercaseInput)).sort((a, b) => {
      if (a.isApproved === b.isApproved) {
        return a.name.localeCompare(b.name); 
      }
      return a.isApproved ? -1 : 1;
    });

    // And we update our state
    setFilteredUserTags(filtered.filter((tag) => !addedUserTags.includes(tag)));
  }
  

  useEffect(() => {
    filterTags();
  }, [inputValue]); // Run update when inputValue changes

  useEffect(() => {
    filterUserTags();
  }, [inputValue]); // Run update when inputValue changes

  return (
    <div className={className}>
      <div className="relative w-full">
        <InputBlock data-testid="popup_text" className="block w-full">
          <div className="flex items-center mt-1">
            <InputHeader className="">Tags: </InputHeader>
            <Button onClick={() => setShowCreateTagField(!showCreateTagField)} className="ml-auto cursor-pointer text-sm" type="button">
              {showCreateTagField ? 'Cancel' : 'Create'}
            </Button>
          </div>

          {showCreateTagField && (
            <div className="mt-1 flex">
              <FInput 
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
                {tag.name}<AdminTag className="h-4 w-4 self-center" />
              </button>
            ))}
            {filteredUserTags.map((tag) => (
              <button data-testid="select_tag" key={tag.name} onClick={addUserTags} type="button" className="flex gap-2 w-full cursor-pointer p-2 text-left transition-colors duration-200 hover:bg-blue-100">
                {tag.name}{tag.isApproved ? <ApprovedTag className="h-4 w-4 self-center" /> : ""}
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
        {addedUserTags.map((tag) => (
          <div key={tag.name} className="flex w-full p-2 text-left transition-colors duration-200">
            <p>{tag.name}</p>
            <Button onClick={deleteUserTags} name={tag.id} className="ml-auto cursor-pointer" type="button">
              Delete
            </Button>
          </div>
        ))}
      </div>

      {/* Hidden inputs to send the selected tags to the server */}
      {addedTags.map((tag, index) => (
        <input key={index} type="hidden" name={`standardizedTags[${index}]`} value={tag.id} />
      ))}

      {/* Hidden inputs to send the selected tags to the server */}
      {addedUserTags.map((tag, index) => (
        <input key={index} type="hidden" name={`userTags[${index}]`} value={tag.id} />
      ))}
    </div>
  );
}
