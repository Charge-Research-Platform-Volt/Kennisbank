"use client";

import React, { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { ChevronsUpDown } from "lucide-react"
import { User } from "@/types/user.type";
import { ListUsersPaged } from "@/actions/userActions";

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
 * @param standardUser - Default value
 * @param selectMult - Whether you can select one or multiple values
 * @param className - Styling from parent component
 *
 * @returns The dropdown box where the user can type and select a user to be selected
 */
export default function SelectUserDropdown({ onChangeAction = () => {}, standardUser: standardUser = null, selectMult = false, className}: { onChangeAction?: (selectedUsers : User[]) => void, standardUser?: User | null, selectMult?: boolean, className?: string}) {  

  // States containing the input value, users returned by the input value, and the user to be merged
  const [filteredUsers, setFilteredUsers] = useState<User[]>([]);
  const [open, setOpen] = React.useState(false)
  const [inputValue, setInputValue] = useState<string>("");
  const [selectedUsers, updateUsers] = useState<User[]>(standardUser ? [standardUser] : []);

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

  // Change the selected user
  const addUsers = (user: User) => {
    // Cannot add more than 1 user if mode is set to selecting a single user
    if(selectedUsers.length != 0 && !selectMult){
      return;
    }

    // As long as the selectedUser exists (it should), update the added user, call the parent function, reset the input value and stop fetching users
    if (user) {
      updateUsers(selectedUsers.concat([user]));
      if(!selectMult)
        setOpen(false);
      setInputValue("");
      setFilteredUsers([]);
    }
  };

  // Deletes a selected user
  const deleteUsers = (event: React.MouseEvent<HTMLButtonElement>) => {
    if(selectedUsers == null)
      return

    // Check if the user to be deleted matches the selected user
    const button = event.currentTarget;

    selectedUsers.forEach(user => {
      if (user.id == button.name) {
        updateUsers(selectedUsers.filter(t => t.id != user.id)); // Return everything aside from this user
        setInputValue("");
        setFilteredUsers([]);
      }
    });
  };

  // Filters users to display only those users that correspond with the input value
  async function filterUsers() {
    // Ensures we don't add more users than allowed and we don't render all users at the start (We want to display filtered users after at least 1 character is in the input)
    if (inputValue == "" || (!selectMult && selectedUsers.length != 0)) {
      setFilteredUsers([]);
      return;
    }

    // Gets all values from inserted users and then filters the users on uppercase name, sorts them on relevance, and returns top 5 users
    const fetchedUsers = (await ListUsersPaged(1, inputValue)); // update this

    // Don't do anything if fetchedUsers returns null or undefined
    if(fetchedUsers.users == null || fetchedUsers.users == undefined)
      return;

    // And we update our state, leaving out any users we already have selected
    setFilteredUsers(fetchedUsers.users.filter(user => !selectedUsers.includes(user)));
  }
  

  useEffect(() => {
    if(inputValue == "")
        setFilteredUsers([])
      else
        filterUsers();  }
    , [inputValue]); // Run update on filter when the inputValue changes

  useEffect(() => {
    updateUsers(standardUser ? [standardUser] : [])
  }, [standardUser]) // Run update on the current selectedUser when we reset the standard user in the parent component

  useEffect(() => {
    // On deletion the selectedUsers will be an empty list, otherwise it will contain all selected users and pass that to the parent component in the function
    onChangeAction(selectedUsers);
  }, [selectedUsers]); // This effect runs every time selectedUsers changes

  return (
    <div className={className}>
    {/* Button that triggers popup to select a user*/}
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-[200px] justify-between"
          data-testid="trigger"
        >
        {selectedUsers.length != 0 && !selectMult ? truncateString(selectedUsers[0].username, 15) : "Select user(s)..."}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
    {/* Popup itself where you can search for users */}
    <PopoverContent className="w-[200px] p-0">
      <Command>
          {/* Searchbox */}
          <CommandInput placeholder="Search for users..." onValueChange={handleInputChange} value={inputValue} disabled={!selectMult && selectedUsers.length != 0}/>
          <CommandList>
            <CommandEmpty>No users.</CommandEmpty>
            {/* Fetched users after typing */}
            <CommandGroup>
              {filteredUsers.map((user) => ( !selectedUsers.some(t => t.id === user.id) &&
                <CommandItem
                  className="cursor-pointer"
                  key={user.id}
                  value={user.username}
                  onSelect={() => addUsers(user)}
                >
                  { user.username }
                </CommandItem>
              ))}
            </CommandGroup>
            {/* Displaying selected user and the delete button */}
            <CommandGroup>
              {
                selectedUsers.map(
                  user =>
                  <CommandItem key={user.id}>
                    {user.username}
                    {
                      <Button data-testid="delete_user" onClick={deleteUsers} name={user.id} className="ml-2 cursor-pointer" type="button">
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