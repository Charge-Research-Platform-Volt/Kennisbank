"use client";

import { Input } from "@/components/ui/input";
import { SaveUserResponse, User } from "@/types/user.type";
import DeleteUserButton from "./delete-account-button";
import { startTransition, useEffect, useState } from "react";
import EditIcon from "@/icons/edit-icon";
import { Button } from "@/components/ui/button";
import SaveIcon from "@/icons/save-icon";
import { SaveUser } from "@/actions/userActions";
import { toast } from "sonner";

export default function UserListItem({ user } : {user: User}) {
    const initialState: SaveUserResponse = {
        success: false,
        message: "",
        user: user,
    }

    const [editting, setEditting] = useState(false);
    const [email, setEmail] = useState(user.email);
    const [role, setRole] = useState(user.role);
    const [userState, setUser] = useState(user);

    const [isSaving, setIsSaving] = useState(false);
    const [saveResponseState, setResponseState] = useState<SaveUserResponse>(initialState);

    //save user when the form is submitted
    const handleSave = (event: React.FormEvent) => {
        try{
            // start transition to avoid blocking the UI
            startTransition(async () => {
                event.preventDefault();
                //set saving state to true
                setIsSaving(true);

                //save the user and get the response
                const response = await SaveUser(saveResponseState, { newEmail: email, newRole: role });

                //save the response to the state so that it can be processed in the useEffect
                setResponseState(response);

                //set saving state to false
                setIsSaving(false);
              });
        }
        catch{
            toast.error("Error saving user.");
        }
      };

    //update the user state when the save response changes
    useEffect(() => {
        //set the user to the result of the save response
        setUser(saveResponseState.user!);

        //check if successful and show a message
        if (saveResponseState.success) {
            toast.success(saveResponseState.message);
            setEditting(false);
        } else if (saveResponseState.message) {
            toast.error(saveResponseState.message);
        }
      }, [saveResponseState]);
      
      //update the email and role when the user state changes
      useEffect(() => {
        if (userState) {
          setEmail(userState.email);
          setRole(userState.role);
        }
      }, [userState]);

    return (
        <div className="flex w-full items-center justify-between gap-2">
            { editting ? 
                /* Item for editting email and role */
                (
                    <form className="relative w-full" onSubmit={handleSave}>
                        <Input
                            type="text"
                            name="name"
                            placeholder="Email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                        />
                        <div className="absolute inset-y-0 right-2 flex items-center justify-center">
                            <select value={role} onChange={(e) => setRole(e.target.value)} style={isSaving ? { pointerEvents: 'none' } : {}}>
                                    <option value="user">User</option>
                                    <option value="admin">Admin</option>
                            </select>
                            <Button
                                className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
                                variant="default"
                                type="submit"
                                title="Save tag"
                                disabled={isSaving}
                            >
                                <SaveIcon className="h-5 w-5" fill="#737373"/>
                            </Button>
                        </div>
                        <Input
                            type="hidden"
                            name="id"
                            value={userState.id}
                        />
                    </form>
                ) : (
                    /* Item for displaying email and role with options for opening edit menu and deleting user */
                    <form className="relative w-full shadow rounded-md px-3 py-1">
                        <div className="flex gap-2">
                            <p>{userState.email}</p>
                        </div>
                        <div className="absolute inset-y-0 right-2 flex items-center justify-center">
                            <select value={role} onChange={(e) => setRole(e.target.value)} className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground" style={{ pointerEvents: 'none' }}>
                                <option value="user">User</option>
                                <option value="admin">Admin</option>
                            </select>
                            <Button
                                className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
                                variant="default"
                                type="submit"
                                title="Edit tag"
                                onClick={(e) => {
                                    e.preventDefault();
                                    setEditting(true);
                                }}
                            >
                                <EditIcon className="h-5 w-5" fill="#737373" />
                            </Button>

                            <DeleteUserButton user={userState} />
                        </div>
                        <Input
                            type="hidden"
                            name="id"
                            value={user.id}
                        />
                    </form>
                )}
        </div>
    )    
}
  

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


