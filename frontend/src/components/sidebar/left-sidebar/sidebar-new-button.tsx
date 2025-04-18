"use client";

import React, { useRef, useEffect, useState, ChangeEvent, useActionState, useTransition } from "react";
import "@/components/ui/Popup.css";
import "@/app/globals.css";
import { AddDocument } from "@/actions/documentActions";
import { AddWebsite } from "@/actions/websiteActions";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { toast } from "sonner";
import { FormResponse } from "@/types/return.type";
import { type FileBase, type WebsiteBase } from "@/types/resource.type";
import { Button } from "@/components/ui/button";
import { getFileHasher } from "@/utils/fileHashWorker";
import { TagArray } from "@/types/tag.type";
import TagSelectionDropdown from "@/components/uploadComponents/TagSelectionDropdown";
import { InputHeader, InputBlock, FInput, FileInfo, PopupTitle } from "@/components/ui/Popup";
import New from "@/icons/new";

type UploadStatus = "idle" | "uploading" | "success" | "error" | "checking";

const initialWebsiteResourceState: FormResponse<WebsiteBase> = {
  success: false,
  message: "",
};

const initialFileResourceState: FormResponse<FileBase> = {
  success: false,
  message: "",
};

export default function NewButton({ tags, asIcon = false }: { tags: TagArray, asIcon?: boolean }) {
  const popupRef = useRef<HTMLDivElement | null>(null); //Ref used to check if user clicks outside of popup
  const [status, setStatus] = useState<UploadStatus>("idle"); //upload status
  const [uploadPopup, setUploadPopup] = useState(false); //bool which determines whether you can see the new popup
  const [newFile, setNewFile] = useState<File | null>(null); //File for file upload
  const [newWebsite, setNewWebsite] = useState<string | null>(null); // Website for website upload
  const [fileHash, setFileHash] = useState<string>(""); // Hash of the file
  const [newUploadType, setUploadType] = useState<uploadType>("File") // Sets upload type of popup
  const [isDuplicate, setIsDuplicate] = useState<boolean>(false); // If file already exists in storage
  const setDupeId = useState<string>("")[1]; // The ID of the file if it already exists in archive

  const [author, setAuthor] = useState<string>("");
  const [description, setDescription] = useState<string>("");
  const [title, setTitle] = useState<string>("");

  const [newButtonWidth, setNewButtonWidth] = useState(0);
  const newButtonRef = React.useRef<HTMLButtonElement>(null);
  useEffect(() => {
    if (newButtonRef.current) {
      setNewButtonWidth(newButtonRef.current.offsetWidth);
    }
  }, []);

  type uploadType = "File" | "Website";

  const [isPendingTransition, startTransition] = useTransition();
  const [fileState, fileAction] = useActionState((prevState: FormResponse<FileBase>, formData: FormData) => {
    const fileName = newFile?.name.substring(0, newFile?.name.lastIndexOf(".")) || "";
    return AddDocument(fileName, fileHash, prevState, formData);
  }, initialFileResourceState);

  const [websiteState, websiteAction] = useActionState((prevState: FormResponse<WebsiteBase>, formData: FormData) => {
    return AddWebsite(prevState, formData);
  }, initialWebsiteResourceState);

  const isPending = isPendingTransition;

  //error messaging //

  useEffect(() => {
    if (fileState.success) {
      toast.success(fileState.message);

      closeUploadPopup();
    } else if (fileState.message) {
      toast.error(fileState.message);
      setStatus("error")
    } else if (fileState.errors) {
      // Get all error arrays from the error object
      Object.values(fileState.errors).forEach(errorsArray => {
        // Each property might be an array of error messages or undefined
        if (errorsArray) {
          errorsArray.forEach(errorMsg => {
            toast.error(errorMsg);
          });
        }
      });
    }
  }, [fileState]);

  useEffect(() => {
    if (websiteState.success) {
      toast.success(websiteState.message);

      closeUploadPopup();
    } else if (websiteState.message) {
      toast.error(websiteState.message);
    } else if (websiteState.errors) {
      // Get all error arrays from the error object
      Object.values(websiteState.errors).forEach(errorsArray => {
        // Each property might be an array of error messages or undefined
        if (errorsArray) {
          errorsArray.forEach(errorMsg => {
            toast.error(errorMsg);
          });
        }
      });
    }
  }, [websiteState]);

  //upload new file popup //
  const closePopup = (e: MouseEvent) => {
    if (popupRef.current && !popupRef.current.contains(e.target as Node)) {
      closeUploadPopup(); // When the mouse is clicked outside of the popup, the popup closes
    }
  };

  const clickNew = (tab: uploadType) => {
    changeTab(tab); // Changes upload tab
    setUploadPopup(true); //opens popup for file upload
  };

  const changeTab = (newTab: uploadType) => {
    if(newTab === newUploadType){
      return;
    }
    setUploadPopup(true);
    setNewFile(null);
    setFileHash("");
    setIsDuplicate(false);
    setDupeId("");
    setAuthor("");
    setDescription("");
    setTitle("");
    setStatus("idle");
    setUploadType(newTab);  // Changes upload tab
  }

  const closeUploadPopup = () => {
    //When the popup closes values are reset
    setUploadPopup(false);
    setNewFile(null);
    setFileHash("");
    setIsDuplicate(false);
    setDupeId("");
    setAuthor("");
    setDescription("");
    setTitle("");
    setStatus("idle");
  };

  useEffect(() => {
    //event listener on mouse used to close popup whenever a mouseclick occurs outside the popup
    if (uploadPopup) {
      document.addEventListener("mousedown", closePopup);
    }
    return () => {
      document.removeEventListener("mousedown", closePopup);
    };
  }, [uploadPopup]);

  async function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    //is called when a file is selected from explorer
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
      } catch (error) {
        console.error("Error checking file: ", error);
        toast.error("Error checking file. Please try again.");
        setStatus("error");
      }
    }
  }

  const handleSubmit = async (formData: FormData) => {
    if (newUploadType === "File" && isDuplicate) {
      toast.error("Cannot upload duplicate file!");
      return;
    }

    if (newUploadType === "File" && !newFile) {
      toast.error("Please select a file.");
      return;
    }

    if(newUploadType === "Website" && newWebsite == null){
      toast.error("Please input a URL!");
    }

    setStatus("uploading");

    try {
      startTransition(async () => {
        if(newUploadType === "File"){
          await fileAction(formData);
        }
        if(newUploadType === "Website"){
          await websiteAction(formData);
        }
      });
    } catch (error) {
      console.error("Upload error: ", error);
      setStatus("error");
    }
  };

  return (
    <div>
      <DropdownMenu>
        {/* Purple New button */}
        <DropdownMenuTrigger
          ref={newButtonRef}
          className={`bg-purple active:bg-purple cursor-pointer text-white hover:bg-[#6f2aaf] w-full p-2 ${asIcon ? "mb-[2vh] rounded rounded-l-none rounded-r-lg transition" : "font-face text-md flex h-9 items-center rounded-md text-left"}`}
        >
          <New className={asIcon ? "h-4 w-4" : "h-5 w-5 mr-1"} />
          {/* If asIcon is true, only show icon */}
          {asIcon ? null : <div data-testid="button_text" className="pb-0.5">New</div>}
        </DropdownMenuTrigger>
        <DropdownMenuContent style={{ width: newButtonWidth }}>
          {/* Upload file item in popup */}
          <DropdownMenuItem data-testid="button_in" className="cursor-pointer" onClick={() => clickNew("File")}>
            <label className="inline-block cursor-pointer">Upload New Document</label>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          {/* Upload website item in popup */}
          <DropdownMenuItem data-testid="button_in" className="cursor-pointer" onClick={() => clickNew("Website")}>
            <label className="inline-block cursor-pointer">Upload New Website</label>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          {/* New project item in popup */}
          <DropdownMenuItem data-testid="button_in" className="cursor-pointer">
            <label className="inline-block cursor-pointer">Create New Project</label>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      {uploadPopup && (
        <div className="popupContainer">

          <div ref={popupRef} className="popup h-full min-h-160 min-w-130 p-5">
            <form
              className="h-full max-h-[100%]"
              onSubmit={(e) => {
                e.preventDefault();
                const formData = new FormData(e.currentTarget);
                handleSubmit(formData);
              }}
            >
              <div className="flex h-full w-full flex-col justify-between">
                <div className="flex-1 justify-start">
                  <div className="flex-1 justify-start">
                  <div className="mb-2">
                    <PopupTitle data-testid="popup_text">Upload {newUploadType}</PopupTitle>
                    </div>
                    <div>
                      {/* Label is what you see however you click the input, only applicable if the user uploads a file */}
                      {newUploadType === "File" && (
                        <div className="mt-5 mb-2 flex w-40">
                          <label htmlFor="file-Picker" className="labelCSS flex h-full w-full min-w-40 cursor-pointer items-center justify-center rounded-xl bg-[#E5E5E5] font-bold hover:bg-[#c9c2c2]">
                          {status === "checking" ? "Checking file..." : "Upload New File"}
                            </label>
                          <input
                            id="file-Picker"
                            name="file"
                            style={{ visibility: "hidden", position: "absolute" }}
                            type="file"
                            onChange={handleFileChange}
                            className="absolute inset-0 h-full w-full cursor-pointer opacity-0"
                            disabled={status === "checking" || status === "uploading"}
                          />
                        </div>)}
                    </div>
                  </div>

                  {/* Information on uploaded file, only applicable if the user uploads a file */}
                  {newUploadType == "File" && (
                  <div>
                    <div className="flex">
                      <FileInfo>
                        Type: {newFile && newFile.type} {!newFile && "-"}
                      </FileInfo>
                      <FileInfo>
                        Size: {newFile && (newFile.size / 1024).toFixed(2)} {!newFile && "-"} KB
                      </FileInfo>
                      <FileInfo>
                        Hash: {fileHash && `${fileHash.substring(0, 10)}...`} {!fileHash && "-"}
                      </FileInfo>
                      {isDuplicate && <p className="font-bold text-red-500">Duplicate file detected!</p>}
                    </div>
                  </div>)}


                  {/* Document title entry */}
                  {newUploadType === "Website" && (
                      <div>
                        <InputBlock className="justify-start">
                          <InputHeader>Website URL: </InputHeader>
                          <FInput className="w-full" type="string" placeholder="Enter URL" name="url" onChange={(e) => setNewWebsite(e.target.value.trim())} />
                        </InputBlock>
                      </div>
                      )}
                  
                  <InputBlock data-testid="popup_text" className="justify-start">
                    <InputHeader>Document Title: </InputHeader>
                    <FInput className="w-full" type="string" placeholder="Enter document title" name="title" value={title} onChange={(e) => setTitle(e.target.value.trimStart())} />
                  </InputBlock>

                  <div className="flex gap-3">
                    {/* Description entry */}
                    <div className="flex-1">
                      <InputBlock data-testid="popup_text">
                        <InputHeader>Description: </InputHeader>
                        <textarea
                          draggable="false"
                          name="description"
                          placeholder="Enter description"
                          maxLength={512}
                          className="h-40 w-full resize-none bg-slate-200 pl-2"
                          value = {description}
                          onChange={(e) => setDescription(e.target.value.trimStart())}
                        />
                      </InputBlock>

                      {/* Author entry */}
                      <InputBlock data-testid="popup_text">
                        <InputHeader className="">Author Name: </InputHeader>
                        <FInput className="w-full" type="string" name="author" placeholder="Enter author name" value={author} onChange={(e) => setAuthor(e.target.value.trimStart())} />
                      </InputBlock>
                    </div>

                    <div className="flex flex-1 flex-col">
                      {/* Add Tags dropdown box*/}
                      <TagSelectionDropdown className="h-full w-full" tags={tags}></TagSelectionDropdown>
                    </div>
                  </div>
                </div>
                <div>
                  <div className="float-right flex justify-end">
                    {/* Upload button */}
                    <Button
                      data-testid="popup_text"
                      type="submit"
                      className="float-right h-10 w-50 cursor-pointer rounded-xl text-lg font-bold"
                      disabled={status === "checking" || isPending || isDuplicate || (!newFile && !newWebsite) || author == "" || title == ""}
                    >
                      <div className="pb-0.5">{isPending ? "Uploading..." : "Upload"}</div>
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
}
