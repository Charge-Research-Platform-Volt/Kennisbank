"use client";

import React, { useState, useEffect } from "react";
import { Tag } from "@/types/tag.type";
import { Button } from "@/components/ui/button";
import AdminTagIcon from "@/icons/tag-icons/admin-tag";
import ApprovedTagIcon from "@/icons/tag-icons/aproved-tag";
import { fetchTagSearch } from "@/actions/tagActions";
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"

import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"

import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popoverWithoutPortal"
/**
 *
 * @param onChangeAction - Which function to call in another component when a value is changed
 * @param standardTag - Default value
 *
 * @returns The dropdown box where the user can type and select a tag to be merged
 */
export default function SelectTagDropdown({ onChangeAction = () => {}, standardTag = null}: { onChangeAction?: (selectedTag : string | null) => void, standardTag?: Tag | null }) {  

  // States containing the input value, tags returned by the input value, and the tag to be merged
  const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
  const [open, setOpen] = React.useState(false)
  const [inputValue, setInputValue] = useState<string>("");
  const [selectedTag, updateTag] = useState<Tag | null>(standardTag);

  // Truncates a string to the length specified minus 3 characters used for adding "..."
  function truncateString(toShorten : string, len : number){
    if(toShorten.length <= len)
      return toShorten
    else{
      return toShorten.slice(0, len - 3) + "..."
    }
  }

  // Update the input value when the user types a character
  const handleInputChange = (val : string) => {
    setInputValue(val);
  };

  // Change the selected tag
  const addTags = (tag: Tag) => {
    // Cannot add more than 1 tag
    if(selectedTag != null){
      return;
    }

    // As long as the selectedTag exists (it should), update the added tag, call the parent function, reset the input value and stop fetching tags
    if (tag) {
      updateTag(tag);
      setOpen(false);
      handleTagSelectionChange(tag); // Call the action passed from the parent component
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
  }, [inputValue]); // Run update on filter when the inputValue changes

  useEffect(() => {
    updateTag(standardTag)
  }, [standardTag]) // Run update on the current selectedTag when we reset the standard tag in the parent component

  return (
    <>
    {/* Button that triggers popup to select a tag*/}
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-[200px] justify-between"
          data-testid="trigger"
        >
        {selectedTag ? truncateString(selectedTag.name, 15) : "Select tag..."}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
    {/* Popup itself where you can search for tags */}
    <PopoverContent className="w-[200px] p-0">
      <Command>
          {/* Searchbox */}
          <CommandInput placeholder="Search for tags..." onValueChange={handleInputChange} disabled={selectedTag !== null}/>
          <CommandList>
            <CommandEmpty>No tags.</CommandEmpty>
            {/* Fetched tags after typing */}
            <CommandGroup>
              {filteredTags.map((tag) => (
                <CommandItem
                  key={tag.id}
                  value={tag.name}
                  onSelect={() => addTags(tag)}
                >
                  <Check
                    className={cn(
                      "mr-2 h-4 w-4",
                      selectedTag?.name === tag.name ? "opacity-100" : "opacity-0"
                    )}
                  />
                  { tag.name }
                  { tag.isStandardized ? <AdminTagIcon className="h-4 w-4 self-center" /> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
                </CommandItem>
              ))}
            </CommandGroup>
            {/* Displaying selected tag and the delete button */}
            <CommandGroup>
              {
                selectedTag &&
                  <CommandItem key={selectedTag.id}>
                    {selectedTag.name}
                    {selectedTag.isStandardized ? <AdminTagIcon className="h-4 w-4"/> : selectedTag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
                    {
                      <Button data-testid="delete_tag" onClick={deleteTags} name={selectedTag.id} className="ml-2 cursor-pointer" type="button">
                        Delete
                      </Button>}
                  </CommandItem>
              }
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  </>
)}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)