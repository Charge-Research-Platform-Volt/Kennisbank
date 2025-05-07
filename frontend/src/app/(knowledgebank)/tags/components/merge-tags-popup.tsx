import { Button } from "@/components/ui/button";
import SelectTagDropdown from "./select-tag-dropdown";
import { Tag } from "@/types/tag.type";
import { useState, useEffect } from "react";
import { MergeTag } from "@/actions/tagActions";
import { Merge } from "lucide-react";
import { toast } from "sonner";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
  } from "@/components/ui/dialog"
  import { Label } from "@/components/ui/label";
/**
 * 
 * @param tag - The tag from the tag-list-item used for filling in the left dropdownbox with a standard value
 * @param extraTag - Tag to be used in the second dropdown box, for now only used in testing
 * @returns An icon in the tag-list-item which when pressed, shows a popup the user can select another tag with to merge
 */
export function TagMergeButton({tag, extraTag = null} : {tag:Tag; extraTag?: Tag | null}){
    const [tag1, setTag1] = useState<string | null>(tag.id); // Selected tag in the left dropdown
    const [tag2, setTag2] = useState<string | null>(extraTag ? extraTag.id : null); // Selected tag in the right dropdown
    const [isOpen, setIsOpen] = useState(false); // Whether or not to display the popup
    const [isMerging, setIsMerging] = useState<boolean>(false); // Whether the tags are currently merging or not
    const [standardTag, resetStandard] = useState<Tag | null>(tag); // This is used to reset the tag in the right dropdown in the popup when you close it
    const [emptyTag, resetEmpty] = useState<Tag | null>(null); // Same as above, but for the right dropdown

    // Merges tags from the tag list
    const mergeTags = async () => {
        // Don't merge if one or the other tag is empty
        if(tag1 == null || tag2 == null){
            toast.error("tag(s) empty");
            return;
        }

        // Don't merge if both tags are the same
        if(tag1 == tag2){
            toast.error("You cannot merge two of the same tags");
            return;
        }

        // Now they are both valid and we start merging
        setIsMerging(true);

        const formData : FormData = new FormData()
        formData.append('tagId1', tag1)
        formData.append('tagId2', tag2)

        // Call the server action
        const result = await MergeTag(formData);
            // If we are successful, close the popup and reload the tag page
            if(result.success){
                toast.success(result.message);
                setIsOpen(false);
                window.dispatchEvent(new Event("tagListUpdated"));
            }
            else{
                toast.error(result.message);
            }
        }

        // Reset state after closing
        useEffect(() => {
            if (!isOpen) {
                setIsMerging(false);
                setTag1(tag.id);
                setTag2(extraTag ? extraTag.id : null);
                resetEmpty(null);
                resetStandard(tag);
            }
        }, [isOpen]);

    return(
        <>
            <Dialog open={isOpen} onOpenChange={setIsOpen} modal>
                <DialogTrigger asChild>
                    <Button variant="outline" data-testid="open">
                        <Merge className= "h-5 w-5" fill= "#737373" />
                    </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-[425px]">
                    <DialogHeader>
                    <DialogTitle>Merge Tags</DialogTitle>
                    <DialogDescription>
                        Choose tags to merge
                    </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4 py-4">
                    <div className="grid grid-cols-4 items-center gap-4">
                        <Label htmlFor="first tag" className="text-right">
                        First Tag
                        </Label>
                        <SelectTagDropdown standardTag={standardTag} onChangeAction={setTag1}></SelectTagDropdown>
                        </div>
                    <div className="grid grid-cols-4 items-center gap-4">
                        <Label htmlFor="second tag" className="text-right">
                        Second Tag
                        </Label>
                        <SelectTagDropdown standardTag={extraTag || emptyTag} onChangeAction={setTag2}></SelectTagDropdown>
                        </div>
                    </div>
                    <DialogFooter>
                    <Button onClick={mergeTags} disabled={isMerging || tag1 == null || tag2 == null} className="w-1/4" data-testid="merge">{isMerging ? "Merging" : "Merge"}</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </>
    )
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)