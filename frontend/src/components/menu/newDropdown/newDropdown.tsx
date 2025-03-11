"use client";

import { useRef, useEffect, useState, ChangeEvent, useActionState, useTransition } from "react";
import '../../ui/Popup.css';
import '@/app/globals.css';
import { AddDocument } from "@/actions/documentActions";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { toast } from "sonner";
import { FormResponse } from "@/types/return.type";
import { DocumentBase } from "@/types/document.type";
import { Button } from "@/components/ui/button";
import { getFileHasher } from "@/utils/fileHashWorker";
import { TagsArray } from "@/types/tag.type";
import TagSelectionDropdown from "./TagSelectionDropdown";
import { InputHeader, InputBlock, FInput, FileInfo, PopupTitle } from "../../ui/Popup";
import Image from "next/image";

type UploadStatus =  "idle" | "uploading" | "success" | "error" | "checking";

const initialState: FormResponse<DocumentBase> = {
  success: false,
  message: "",
};

export default function NewButton({tags}: {tags: TagsArray}) {

    const popupRef = useRef<HTMLDivElement | null>(null);       //Ref used to check if user clicks outside of popup
    const [status, setStatus] = useState<UploadStatus>("idle"); //upload status
    const [uploadPopup, setUploadPopup] = useState(false);      //bool which determines whether you can see the new popup
    const [newFile, setNewFile] = useState<File | null>(null);  //File for file upload
    const [fileHash, setFileHash] = useState<string>("");       // Hash of the file
    const [isDuplicate, setIsDuplicate] = useState<boolean>(false); // If file already exists in storage
    const [dupeId, setDupeId] = useState<string>("");           // The ID of the file if it already exists in archive

    const [author, setAuthor] = useState<string>("");
    const [description, setDescription] = useState<string>("");
    const [title, setTitle] = useState<string>("");

    const [isPendingTransition, startTransition] = useTransition();
    const [state, action] = useActionState(
      (prevState: FormResponse<DocumentBase>, formData: FormData) => {
        const fileName = newFile?.name.substring(0, newFile?.name.lastIndexOf(".")) || "";
        return AddDocument(fileName, fileHash, prevState, formData)
      }, initialState);
        
    const isPending = isPendingTransition;

  //error messaging //

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
      closeUploadPopup();
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state]);

  //upload new file popup //
  const closePopup = (e: MouseEvent) => {
    if (popupRef.current && !popupRef.current.contains(e.target as Node)) {
      closeUploadPopup(); // When the mouse is clicked outside of the popup, the popup closes
    }
  };

  const clickNew = () => {
          setUploadPopup(true); //opens popup for file upload
  };
  
  const closeUploadPopup = () => { //When the popup closes values are reset
      setUploadPopup(false);
  } 


  useEffect(() => { //event listener on mouse used to close popup whenever a mouseclick occurs outside the popup
    if (uploadPopup) {
      document.addEventListener("mousedown", closePopup);
    }
    return () => {
      document.removeEventListener("mousedown", closePopup);
    };
  }, [uploadPopup]);

  
  async function handleFileChange(e: ChangeEvent<HTMLInputElement>) { //is called when a file is selected from explorer
    if (e.target.files && e.target.files.length > 0) {
      const file = e.target.files[0];
      setNewFile(file);
      setStatus("checking");
      
      // Hash the file and check if it already exists in the database
      try {
        const fileHasher = getFileHasher();
        const result = await fileHasher.checkDuplicate(file);

        setFileHash(result.hash);
        setIsDuplicate(result.isDuplicate);

        if (result.isDuplicate) {
          toast.warning("This file might already exists in the archive.");
          setDupeId(result.id);
        }
        setStatus("idle");
      }catch (error) {
        console.error("Error checking file: ", error);
        toast.error("Error checking file. Please try again.");
        setStatus("error");
      }
    }
  }

  const handleSubmit = async (formData: FormData) => {
    if (isDuplicate) {
      toast.error("Cannot upload duplicate file!");
      return;
    }

    if (!newFile) {
      toast.error("Please select a file.");
      return;
    }

    setStatus("uploading");

    try {
      startTransition(async () => {
        await action(formData);
      });
    }catch (error) {
      console.error("Upload error: ", error);
      setStatus("error");
    }
  };

  return (
    <div>
      <DropdownMenu>
            {/* Purple New button */}
            <DropdownMenuTrigger className="font-face h-9 w-full bg-purple text-white text-md rounded-md text-left pl-3 
                           hover:bg-[#6f2aaf] active:bg-purple flex items-center gap-1 cursor-pointer">
                <Image 
                      src="img/new-icon.svg"
                      alt="Upload New Document"
                      width={20} 
                      height={20}
                      className="mr-0.5"
                  />
                <div className='pb-0.5'>New</div>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
                {/* Upload item in popup */}
                <DropdownMenuItem className="w-auto" onClick={clickNew}>
                    <label className="inline-block cursor-pointer">
                        Upload New Document
                    </label>
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                {/* New project item in popup */}
                <DropdownMenuItem>
                    <label className="inline-block cursor-pointer">
                      Create New Project
                    </label>
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>





      {uploadPopup && (
      <div className="popupContainer">
        <div ref={popupRef} className="popup p-5">
          <form
            className="max-h-[100%] h-full"
            onSubmit={(e) => {
            e.preventDefault();
            const formData = new FormData(e.currentTarget);
            handleSubmit(formData);
          }}>

          <div className="flex">
            <PopupTitle>Upload Document</PopupTitle>
            <div className="w-2/10 h-10 ml-5 flex">
            {/* Label is what you see however you click the input */}
              <label htmlFor="file-Picker" className="labelCSS font-bold bg-[#E5E5E5] hover:bg-[#c9c2c2] rounded-xl flex items-center justify-center w-full h-full cursor-pointer" >{status === "checking" ? "Checking file..." : "Upload New File"}</label>
              <input id="file-Picker" name="file" style={{visibility:"hidden", position:"absolute"}}
                        type='file' onChange={handleFileChange} className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" disabled={status === "checking" || status === "uploading"} />
            </div>
          </div>


          {/* Document title entry */}
          <InputBlock>
            <InputHeader>Document Title: </InputHeader>
            <FInput className="w-9/10" type="string" placeholder="Enter document title" name="name" onChange={(e) => setTitle(e.target.value.trim())}/>
          </InputBlock>

          {/* Information on uploaded file */}
          <div className="h-6">
            {newFile && (
            <div className="flex">
              <FileInfo>Type: {newFile.type}</FileInfo>
              <FileInfo>Size: {(newFile.size / 1024).toFixed(2)} KB</FileInfo>
              {isDuplicate && ( <p className="text-red-500 font-bold">Duplicate file detected!</p>  )}
              {fileHash &&    ( <FileInfo>Hash: {fileHash.substring(0,10)}...</FileInfo>              )}
            </div>
            )}
          </div>

          <div className="flex w-full h-[100%]">
            {/* Description entry */}
            <div className="flex-1">
              <InputBlock>
                <InputHeader>Description: </InputHeader>
                <textarea draggable='false' name="description" placeholder="Enter description" maxLength={512} className="bg-slate-200 w-9/10 h-40 pl-2 resize-none" onChange={(e) => setDescription(e.target.value.trim())}/>
              </InputBlock>

              {/* Add Tags dropdown box*/}
              <TagSelectionDropdown className="w-9/10" tags={tags}></TagSelectionDropdown>
            </div>

            <div className="flex-1 flex flex-col justify-between">
              {/* Author entry */}
              <InputBlock>
                <InputHeader className="ml-3">Author Name: </InputHeader>
                <FInput  className="w-8/10 ml-3" type="string" name="author" placeholder="Enter author name" onChange={(e) => setAuthor(e.target.value.trim())} />
              </InputBlock>
              <div className="flex justify-end mt-auto">
                {/* Upload button */}
                <Button 
                  type='submit'
                  className="float-right mr-23 w-50 h-10 text-lg font-bold rounded-xl cursor-pointer" 
                  disabled={status === "checking" || 
                      isPending || 
                    isDuplicate ||
                    !newFile ||
                    (author == "") ||
                    (title == "")}>
                    <div className='pb-0.5'>{isPending ? "Uploading..." : "Upload"}</div>
                </Button>
              </div>
            </div>
           


            <input type="hidden" name="hash" value={fileHash} />

          </div>

          </form>
        </div>
      </div> 
      )}

    </div>
  );
};
