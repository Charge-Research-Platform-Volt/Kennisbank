"use client";

import React, { useState, useEffect } from "react";
import { Tag, TagArraySchema } from "@/types/tag.type";
import { Button } from "@/components/ui/button";
import AdminTagIcon from "@/icons/tag-icons/admin-tag";
import ApprovedTagIcon from "@/icons/tag-icons/aproved-tag";
import { fetchTagSearch } from "@/actions/tagActions";
import { ChevronsUpDown } from "lucide-react"

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
 * @param selectMult - Whether you can select one or multiple values
 * @param className - Styling from parent component
 *
 * @returns The dropdown box where the user can type and select a tag to be merged
 */
export default function SelectTagDropdown({ onChangeAction = () => {}, standardTag = null, selectMult = false, className}: { onChangeAction?: (selectedTags : Tag[]) => void, standardTag?: Tag | null, selectMult?: boolean, className?: string}) {  

  // States containing the input value, tags returned by the input value, and the tag to be merged
  const [filteredTags, setFilteredTags] = useState<Tag[]>([]);
  const [open, setOpen] = React.useState(false)
  const [inputValue, setInputValue] = useState<string>("");
  const [selectedTags, updateTags] = useState<Tag[]>(standardTag ? [standardTag] : []);

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
    // Cannot add more than 1 tag if mode is set to selecting a single tag
    if(selectedTags.length != 0 && !selectMult){
      return;
    }

    // As long as the selectedTag exists (it should), update the added tag, call the parent function, reset the input value and stop fetching tags
    if (tag) {
      updateTags(selectedTags.concat([tag]));
      if(!selectMult)
        setOpen(false);
      setInputValue("");
      setFilteredTags([]);
    }
  };

  // Deletes a selected tag
  const deleteTags = (event: React.MouseEvent<HTMLButtonElement>) => {
    if(selectedTags == null)
      return

    // Check if the tag to be deleted matches the selected tag
    const button = event.currentTarget;

    selectedTags.forEach(tag => {
      if (tag.id == button.name) {
        updateTags(selectedTags.filter(t => t.id != tag.id)); // Return everything aside from this tag
        setInputValue("");
        setFilteredTags([]);
      }
    });
  };

  // Filters tags to display only those tags that correspond with the input value
  async function filterTags() {
    // Ensures we don't add more tags than allowed and we don't render all tags at the start (We want to display filtered tags after at least 1 character is in the input)
    if (inputValue == "" || (!selectMult && selectedTags.length != 0)) {
      setFilteredTags([]);
      return;
    }

    // Gets all values from inserted tags and then filters the tags on uppercase name, sorts them on relevance, and returns top 5 tags
    const fetchedTags = TagArraySchema.parse((await fetchTagSearch(inputValue, 5)).body['tags']);

    // Don't do anything if fetchedTags returns null or undefined
    if(fetchedTags == null || fetchedTags == undefined)
      return;

    // And we update our state, leaving out any tags we already have selected
    setFilteredTags(fetchedTags.filter(tag => !selectedTags.includes(tag)));
  }
  

  useEffect(() => {
    if(inputValue == "")
      setFilteredTags([])
    else
      filterTags();
  }, [inputValue]); // Run update on filter when the inputValue changes

  useEffect(() => {
    updateTags(standardTag ? [standardTag] : [])
  }, [standardTag]) // Run update on the current selectedTag when we reset the standard tag in the parent component

  useEffect(() => {
    // On deletion the selectedTags will be an empty list, otherwise it will contain all selected tags and pass that to the parent component in the function
    onChangeAction(selectedTags);
  }, [selectedTags]); // This effect runs every time selectedTags changes

  return (
    <div className={className}>
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
        {selectedTags.length != 0 && !selectMult ? truncateString(selectedTags[0].name, 15) : "Select tag(s)..."}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
    {/* Popup itself where you can search for tags */}
    <PopoverContent className="w-[200px] p-0">
      <Command>
          {/* Searchbox */}
          <CommandInput placeholder="Search for tags..." onValueChange={handleInputChange} value={inputValue} disabled={!selectMult && selectedTags.length != 0}/>
          <CommandList>
            <CommandEmpty>No tags.</CommandEmpty>
            {/* Fetched tags after typing */}
            <CommandGroup>
              {filteredTags.map((tag) => ( !selectedTags.some(t => t.id === tag.id) &&
                <CommandItem
                  className="cursor-pointer"
                  key={tag.id}
                  value={tag.name}
                  onSelect={() => addTags(tag)}
                >
                  { tag.name }
                  { tag.isStandardized ? <AdminTagIcon className="h-4 w-4 self-center" /> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
                </CommandItem>
              ))}
            </CommandGroup>
            {/* Displaying selected tag and the delete button */}
            <CommandGroup>
              {
                selectedTags.map(
                  tag =>
                  <CommandItem key={tag.id}>
                    {tag.name}
                    {tag.isStandardized ? <AdminTagIcon className="h-4 w-4"/> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : "" }
                    {
                      <Button data-testid="delete_tag" onClick={deleteTags} name={tag.id} className="ml-2 cursor-pointer" type="button">
                        Delete
                      </Button>}
                  </CommandItem>
                )
              }
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  </div>
)}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)