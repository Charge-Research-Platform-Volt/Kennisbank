import { Button } from "@/components/ui/button";
import SelectTagDropdown from "./select-tag-dropdown";
import { Tag } from "@/types/tag.type";
import { useState, useEffect, useRef } from "react";
import { MergeTag } from "@/actions/tagActions";
import { Merge } from "lucide-react";
import { toast } from "sonner";
import { PopupTitle } from "@/components/ui/Popup";

/**
 * 
 * @param tag - The tag from the tag-list-item used for filling in the left dropdownbox with a standard value
 * @returns An icon in the tag-list-item which when pressed, shows a popup the user can select another tag with to merge
 */
export function TagMergeButton({tag} : {tag:Tag}){
    const [isOpen, setIsOpen] = useState(false); // Whether or not to display the popup
    const [tag1, setTag1] = useState<string | null>(tag.id); // Selected tag in the left dropdown
    const [tag2, setTag2] = useState<string | null>(null); // Selected tag in the right dropdown
    const [isMerging, setIsMerging] = useState<boolean>(false); // Whether the tags are currently merging or not
    const mergeButtonRef = useRef<HTMLDivElement>(null); // Ref that looks if you click outside the popup
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
                closeMergePopup();
                window.dispatchEvent(new Event("tagListUpdated"));
            }
            else{
                toast.error(result.message);
            }
        }

        // Used to determine if we can close the popup
        const closePopup = (e: MouseEvent) => {
            if (mergeButtonRef.current && !mergeButtonRef.current.contains(e.target as Node)) {
              closeMergePopup(); // When the mouse is clicked outside of the popup, the popup closes
            }
        };

        // Closes the popup
        const closeMergePopup = () => {
            //When the popup closes values are reset
            setIsOpen(false);
            setIsMerging(false);
            setTag1(tag.id);
            setTag2(null);
            resetEmpty(null);
            resetStandard(tag);
        };

        useEffect(() => {
            //event listener on mouse used to close popup whenever a mouseclick occurs outside the popup
            if (isOpen) {
                document.addEventListener("mousedown", closePopup);
            }
            return () => {
                document.removeEventListener("mousedown", closePopup);
            };
        }, [isOpen]);

    return(
            <div>
                <Button
                        className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
                        variant="default"
                        type="button"
                        onClick={() => setIsOpen(!isOpen)}
                        disabled={isOpen}
                    >
                        <Merge className= "h-5 w-5" fill= "#737373" />
                </Button>
                <div className="fixed h-[100vh] w-[100vh]">
                    {
                        isOpen && (
                            <div ref={mergeButtonRef} className="fixed bg-[#fefefe] top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 h-[50vh] w-[60vh] border rounded shadow-md">
                                <div className="flex flex-col gap-10 mt-7 ">
                                    <PopupTitle className="flex justify-center">Merge</PopupTitle>
                                    <div className="flex justify-center gap-4">
                                        <SelectTagDropdown standardTag={standardTag} onChangeAction={setTag1}></SelectTagDropdown>
                                        <SelectTagDropdown standardTag={emptyTag} onChangeAction={setTag2}></SelectTagDropdown>
                                    </div>
                                    <Button onClick={mergeTags} disabled={isMerging || tag1 == null || tag2 == null}>{isMerging ? "Merging" : "Merge"}</Button>
                                </div>
                            </div>
                        )
                    }

                </div>
            </div>
    )
};