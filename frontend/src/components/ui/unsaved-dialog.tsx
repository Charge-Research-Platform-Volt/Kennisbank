import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog"

interface UnsavedDialogProps 
{
    open?: boolean;
    onOpenChange?: (value: boolean) => void;
    onConfirmation?: () => void;
    onCancel?: () => void;
}

export default function UnsavedDialog({ open, onOpenChange, onConfirmation, onCancel }: UnsavedDialogProps) 
{
    return (
        <AlertDialog open={open} onOpenChange={onOpenChange}>
            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>Are you sure?</AlertDialogTitle>
                    <AlertDialogDescription>
                        You have unsaved changes that will be lost if you continue.
                        Are you sure you want to continue?
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel onClick={() => { if (onOpenChange) onOpenChange(false); if (onCancel) onCancel}} className="cursor-pointer">
                        Cancel
                    </AlertDialogCancel>
                    <AlertDialogAction onClick={onConfirmation} className="cursor-pointer">
                        Discard Changes
                    </AlertDialogAction>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    )
}