"use client";

import React, { useState, useEffect } from "react";
import { Tag, TagArraySchema } from "@/types/tag.type";
import { Button } from "@/components/ui/button";
import AdminTagIcon from "../../icons/tag-icons/admin-tag"
import ApprovedTagIcon from "../../icons/tag-icons/approved-tag"
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
import { ListUsersPaged } from "@/actions/userActions";
import { User } from "@/types/user.type";
import { useDebouncedCallback } from "use-debounce";

// An item here has at least an id or name
export interface Item {
    id: string;
    name: string; // This is displayed in the actual dropdown
}

// A UserItem is an item with a username
export interface UserItem extends Item {
    username: string;
}

// A TagItem is an item with the isStandardized or isApproved property
export interface TagItem extends Item {
    isStandardized?: boolean;
    isApproved?: boolean;
}

interface SelectDropdownProps<T extends Item> {
    // Which function to call in another component when a value is changed
    onChangeAction?: (selectedItems: T[]) => void;
    // Standard item(s) to pre-select
    standardItem?: T | null;
    // Boolean indicating whether or not you can select multiple values
    selectMultiple?: boolean;
    // Optional styling from parent component
    className?: string;
    // Function used to fetch the data used to make a selection in the dropdown box
    fetchFunction: (inputValue: string) => Promise<T[]>;
    // Function to get the display value for the PopoverTrigger button
    getTriggerDisplay: (selectedItems: T[], selectMultiple: boolean, truncateString: (s: string, len: number) => string) => string;
    // Placeholder text for the search input
    placeholder?: string;
    // Optional rendering for items in the filtered list (e.g., icons next to names)
    renderItem?: (item: T) => React.ReactNode;
    // Optional rendering for items in the selected list (e.g., icons next to names, delete button)
    renderSelectedItem?: (item: T, onDelete: (id: string) => void) => React.ReactNode;
}

/**
 * Selection box where the user can select one or multiple items.
 * 
 * @author Justin Liem
 * @param {() => void} onChangeAction - Which function to call in another component when a value is changed
 * @param {T | null} standardItem - Default value
 * @param {boolean} selectMultiple - Whether you can select one or multiple values
 * @param {string} className - Styling from parent component
 * @param {string => Promise<T[]>} fetchFunction - Function used to fetch the data used to make a selection in the dropdown box
 * @param {(T[], boolean, (string, number) => string) => string} getTriggerDisplay - Function to get the display value for the PopoverTrigger button
 * @param {string} placeholder - Placeholder text for the search input
 * @param {T => React.ReactNode} renderItem - Optional rendering for items in the filtered list (e.g., icons next to names)
 * @param {(T, (string => void) => React.ReactNode)} renderSelectedItem - Optional rendering for items in the selected list (e.g., icons next to names, delete button)
 *
 * @returns The dropdown box where the user can type and select a item to be merged
 */
function SelectDropDown<T extends Item>({
    onChangeAction = () => {},
    standardItem = null,
    selectMultiple = false,
    className,
    fetchFunction,
    getTriggerDisplay,
    placeholder = "Search...",
    renderItem,
    renderSelectedItem,
}: SelectDropdownProps<T>) {

// States containing the input value, items returned by the input value, and the item to be merged
const [filteredItems, setFilteredItems] = useState<T[]>([]);
const [open, setOpen] = React.useState(false)
const [inputValue, setInputValue] = useState<string>("");
const [selectedItems, updateItems] = useState<T[]>(standardItem ? [standardItem] : []);

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

  // Change the selected item
const addItem = (item: T) => {
    // Cannot add more than 1 item if mode is set to selecting a single item
    if(selectedItems.length != 0 && !selectMultiple){
        return;
    }

    // As long as the selectedTag exists (it should), update the added item, call the parent function, reset the input value and stop fetching items
    if (item) {
        updateItems(selectedItems.concat([item]));
        if(!selectMultiple)
            setOpen(false);
        setInputValue("");
        setFilteredItems([]);
    }
};

  // Deletes a selected item
const deleteItem = (id: string) => {
    if(selectedItems == null || selectedItems.length == 0)
        return;

    // Check if the item to be deleted matches any of the selected item
    selectedItems.forEach(item => {
        if (item.id == id) {
            updateItems(selectedItems.filter(t => t.id != item.id)); // Return everything aside from this item
            setInputValue("");
            setFilteredItems([]);
    }
    });
};

// Filters items to display only those items that correspond with the input value
// A debounced callback is used so that the filter doesn't always immediately rerender the whole component
const filterItems = useDebouncedCallback( async() => {
    // Ensures we don't add more items than allowed and we don't render all items at the start (We want to display filtered items after at least 1 character is in the input)
    if (inputValue == "" || (!selectMultiple && selectedItems.length != 0)) {
        setFilteredItems([]);
        return;
    }

    // Gets all values from inserted items and then filters the items on uppercase name, sorts them on relevance, and returns top 5 items
    const fetchedItems = await fetchFunction(inputValue);

    // Don't do anything if fetchedTags returns null or undefined
    if(fetchedItems == null || fetchedItems == undefined)
        return;

    // And we update our state, leaving out any items we already have selected
    setFilteredItems(fetchedItems.filter(item => !selectedItems.includes(item)));
    }, 200);


useEffect(() => {
    if(inputValue == "")
        setFilteredItems([])
    else
        filterItems();
  }, [inputValue, filterItems]); // Run update on filter when the inputValue changes

useEffect(() => {
    updateItems(standardItem ? [standardItem] : [])
  }, [standardItem]) // Run update on the current selectedTag when we reset the standard item in the parent component

useEffect(() => {
    // On deletion the selectedTags will be an empty list, otherwise it will contain all selected items and pass that to the parent component in the function
    onChangeAction(selectedItems);
  }, [selectedItems, onChangeAction]); // This effect runs every time selectedTags changes

return (
    <div className={className}>
        {/* Button that triggers popup to select a item*/}
        <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
            <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className="w-[200px] justify-between"
            data-testid="trigger"
            >
            { getTriggerDisplay(selectedItems, selectMultiple, truncateString) }
            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
            </Button>
        </PopoverTrigger>
        {/* Popup itself where you can search for items */}
        <PopoverContent className="w-[200px] p-0">
        <Command>
            {/* Searchbox */}
            <CommandInput placeholder={ placeholder } onValueChange={handleInputChange} value={inputValue} disabled={!selectMultiple && selectedItems.length != 0}/>
            <CommandList>
                <CommandEmpty>
                    {inputValue === "" ? "Start typing..." : "No Results."}
                </CommandEmpty>
                {/* Fetched items after typing */}
                <CommandGroup>
                {filteredItems.map((item) => ( !selectedItems.some(t => t.id === item.id) &&
                    <CommandItem
                    className="cursor-pointer"
                    key={item.id}
                    value={item.name}
                    onSelect={() => addItem(item)}
                    >
                    { renderItem ? renderItem(item) : item.name}
                    </CommandItem>
                ))}
                </CommandGroup>
                {/* Displaying selected item and the delete button */}
                <CommandGroup>
                {
                    selectedItems.map(
                    item =>
                    <CommandItem key={item.id}>
                        {renderSelectedItem ? (renderSelectedItem(item, deleteItem)) : (
                        <>
                            {item.name}
                            <Button
                            onClick={() => deleteItem}
                            className="ml-2 cursor-pointer"
                            type="button"
                            name={item.id}
                            data-testid={`delete_${item.name.toLowerCase()}`}
                            >
                            Delete
                            </Button>
                        </>
                        )}
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

/* The following functions are for creating the tag selection dropdown box */
const displayTags = (selectedTags: Tag[], selectMultiple: boolean, truncate: (s: string, len: number) => string) => {
    if (selectedTags.length > 0 && !selectMultiple) {
        return truncate(selectedTags[0].name, 15);
    }
    return "Select tag(s)...";
};

const fetchTags = async (inputValue: string): Promise<Tag[]> => {
    const fetched = TagArraySchema.parse((await fetchTagSearch(inputValue, 5)).body['tags']);
    return fetched;
};

/**
 * Selection box where the user can select one or multiple tags.
 * 
 * @author Justin Liem
 * @param {tag[] => void} onChangeAction - Which function to call in another component when the tag selection is changed
 * @param {Tag | null} standardTag - Default tag
 * @param {boolean} selectMultiple - Whether you can select one or multiple tags
 * @param {string} className - Styling from parent component
 * @returns Selection box where the user can select tags
 */
export function SelectTagDropdown({onChangeAction = () => {}, className, selectMultiple, standardTag} : {onChangeAction?: (selectedTags : Tag[]) => void, className?: string, selectMultiple: boolean, standardTag?: Tag})
{
    return(
        <SelectDropDown
            onChangeAction={onChangeAction}
            fetchFunction={fetchTags}
            selectMultiple={selectMultiple}
            className={className ?? ""}
            standardItem={standardTag}
            getTriggerDisplay={displayTags}
            placeholder={"Search for tags..."}
            renderItem={(tag: Tag) => (
                <div className="flex items-center gap-2">
                    {tag.isStandardized ? <AdminTagIcon className="h-4 w-4" /> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : null}
                        <span>{tag.name}</span>
                </div>
            )}
            renderSelectedItem={(tag: Tag, onDelete) => (
                <div className="flex items-center justify-between w-full overflow-x-auto">
                    <div className="flex items-center gap-2">
                        {tag.isStandardized ? <AdminTagIcon className="h-4 w-4" /> : tag.isApproved ? <ApprovedTagIcon className="h-4 w-4" /> : null}
                            <span>{tag.name}</span>
                    </div>
                    <Button onClick={() => onDelete(tag.id)} size="sm" className="ml-2 cursor-pointer" type="button">
                        Delete
                    </Button>
                </div>
            )}></SelectDropDown>
    )
}

/* The following functions are for creating the user selection dropdown box */
type DisplayUser = User & { name: string };

const displayUsers = (selectedUsers: User[], selectMultiple: boolean, truncate: (s: string, len: number) => string) => {
    if (selectedUsers.length > 0 && !selectMultiple) {
        return truncate(selectedUsers[0].username, 15);
    }
    return "Select user(s)...";
};

const fetchUsers = async (inputValue: string, exclude: string[]): Promise<DisplayUser[]> => {
    const fetchedUsers = (await ListUsersPaged(1, inputValue)).users?.filter(user => !exclude.includes(user.id)) ?? [];
    return fetchedUsers.map(user => ({ ...user, name: user.username })); // assign name property so that it can be found in the selection box
};

/**
 * Selection box where the user can select one or multiple creators.
 * 
 * @author Justin Liem
 * @param {DisplayUser[] => void} onChangeAction - Which function to call in another component when the creator selection is changed
 * @param {boolean} selectMultiple - Whether you can select one or multiple creators
 * @param {string} className - Styling from parent component
 * @param {string} currentUserId - Current user id, exclude user associated with this id as it will be always added in the backend anyway
 * @returns Selection box where the user can select creators
 */
export function SelectUserDropdown({onChangeAction = () => {}, className, selectMultiple, currentUserId} : {onChangeAction?: (selectedUser : DisplayUser[]) => void, className: string, selectMultiple: boolean, currentUserId?: string})
{
    return(
        <SelectDropDown
            onChangeAction={onChangeAction}
            fetchFunction={val => fetchUsers(val, [currentUserId ?? ""])}
            selectMultiple={selectMultiple}
            className={className ?? ""}
            getTriggerDisplay={displayUsers}
            placeholder={"Search for users..."}
            renderItem={(user) => (
                <div className="flex items-center gap-2">
                    <span>{user.name}</span>
                </div>
            )}
            renderSelectedItem={(user: User, onDelete) => (
                <div className="flex items-center justify-between w-full overflow-x-auto">
                    <span>{user.username}</span>
                    <Button onClick={() => onDelete(user.id)} size="sm" className="ml-2 cursor-pointer" type="button">
                        Delete
                    </Button>
                </div>
            )}></SelectDropDown>
    )
}

// Slightly different from above, since it has multiple users to exclude, namely all the creators
/**
 * Selection box where the user can select one or multiple creators to add to already existing creators.
 * 
 * @author Justin Liem
 * @param {DisplayUser[] => void} onChangeAction - Which function to call in another component when the creator selection is changed
 * @param {boolean} selectMultiple - Whether you can select one or multiple creators
 * @param {string} className - Styling from parent component
 * @param {uesr[]} currentCreators - Current creators of the project
 * @returns Selection box where the user can select creators to add to the already existing creators list.
 */
export function AddUserDropdown({onChangeAction = () => {}, className, selectMultiple, currentCreators} : {onChangeAction?: (selectedUser : DisplayUser[]) => void, className: string, selectMultiple: boolean, currentCreators: User[]})
{
    return(
        <SelectDropDown
            onChangeAction={onChangeAction}
            fetchFunction={(val => fetchUsers(val, currentCreators.map(c => c.id)))} // filter out any already added creators
            selectMultiple={selectMultiple}
            className={className ?? ""}
            getTriggerDisplay={displayUsers}
            placeholder={"Search for users..."}
            renderItem={(user) => (
                <div className="flex items-center gap-2">
                    <span>{user.name}</span>
                </div>
            )}
            renderSelectedItem={(user: User, onDelete) => (
                <div className="flex items-center justify-between w-full overflow-x-auto">
                    <span>{user.username}</span>
                    <Button onClick={() => onDelete(user.id)} size="sm" className="ml-2 cursor-pointer" type="button">
                        Delete
                    </Button>
                </div>
            )}></SelectDropDown>
    )
}
// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)