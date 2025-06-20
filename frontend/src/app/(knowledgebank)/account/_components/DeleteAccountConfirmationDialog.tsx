"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import React from "react";
import { toast } from "sonner";
import { DeleteOwnAccount } from "@/actions/userActions";
import { Logout } from "@/actions/authActions";

/**
 * @summary Simple dialog with a single input field to use to create new resources.
 * @param open Determines if the dialog is open
 * @param onOpenChange This function is called when the open state changes
 * @param title The title of the dialog
 * @param placeholder The placeholder for the input field in the dialog
 * @param onCreate This function is called when the create button is clicked
 * @returns 
 */
export default function DeleteAccountConfirmationDialog({open = false, onOpenChange}: { open?: boolean, onOpenChange?: (open: boolean) => void }) 
{
    const [isDeleting, setIsDeleting] = React.useState<boolean>(false);
    
    async function onSubmit() 
    {
        setIsDeleting(true);

        const response = await DeleteOwnAccount();

        if(response.success) {
            if(onOpenChange) onOpenChange(false);
            toast.success("Account deleted successfully");
            const result = await Logout();
        
            if (result.success) window.location.reload();
            else console.error(result.message);
        }
        else{
            toast.error("Failed to delete account. Please try again later.");
        }
        setIsDeleting(false);
    }

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>Delete account</DialogTitle>
                </DialogHeader>
                
                <p className="mb-4 text-sm text-gray-500">
                    Are you sure you want to delete your account? This action cannot be undone.
                </p>
                
                <DialogFooter className="flex">
                    <Button type="button" onClick={() => onOpenChange && onOpenChange(false)} disabled={isDeleting}>Cancel</Button>
                    <Button className="ml-auto bg-red-500 hover:bg-red-400" type="button" onClick={onSubmit} disabled={isDeleting}>{isDeleting ? "Deleting account..." : "Confirm deleting account"}</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


