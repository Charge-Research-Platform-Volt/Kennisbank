"use client";

import { Button } from "@/components/ui/button";
import { User } from "@/types/user.type";
import { useTransition } from "react";
import { toast } from "sonner";
import { DeleteUser } from "@/actions/userActions";
import DeleteIcon from "@/icons/delete-icon";

export default function DeleteUserButton({user} : {user: User}) {
    const [isPending, startTransition] = useTransition();

    //delete user when the button is clicked
    function handleDelete() {
        startTransition(async () => {
            try{
                // run the delete action
                const result = await DeleteUser(user);

                //check if the result is successful and show a message
                if(result && result.success){
                    toast.success("User deleted");
                    
                    //reload the page to show the changes
                    window.location.reload();
                }
                else if(result && result.message) {
                    toast.error(result.message);
                }
                else{
                    toast.error("Error deleting user");
                }
            }
            catch {
                toast.error("Error deleting user");
            }
        });

    }
  
    return (
        <Button
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="button"
          onClick={handleDelete}
          disabled={isPending}
          title="Delete tag"
        >
          <DeleteIcon className="h-5 w-5" fill="#737373" />
        </Button>
    );
  }
  

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


