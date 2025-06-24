import { AlertDialog, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog"
import { Button } from "./button";
import React from "react";

interface ConfirmDeleteDialogProps 
{
    open?: boolean;
    onOpenChange?: (value: boolean) => void;
    onConfirmation?: () => void;
    onCancel?: () => void;
}

/**
 * @summary Simple dialog that asks for confirmation to leave page when there are unsaved changes.
 * @param open Determines if the dialog is open
 * @param onOpenChange This function is called when the open state changes.
 * @param onConfirmation This function is called when the confirm button is pressed
 * @param onCancel This function is called when the cancel button is pressed
 */
export default function ConfirmDeleteDialog({ open, onOpenChange, onConfirmation, onCancel }: ConfirmDeleteDialogProps) 
{
    const handleClose = () => 
    {
        onOpenChange?.(false);
    }
    
    const handleCancel = () => 
    {
        handleClose();
        onCancel?.();
    }
    
    const handleConfirm = () => 
    {
        handleClose();
        onConfirmation?.();
    }

    return (
        <AlertDialog open={open} onOpenChange={onOpenChange}>
            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>Are you sure?</AlertDialogTitle>
                    <AlertDialogDescription>
                        This item will be moved to trash and will be permanently deleted after 30 days.
                        Are you sure you want to continue?
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel onClick={handleCancel} className="cursor-pointer">
                        Cancel
                    </AlertDialogCancel>
                    <Button variant="destructive" onClick={handleConfirm} className="cursor-pointer">Delete</Button>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


