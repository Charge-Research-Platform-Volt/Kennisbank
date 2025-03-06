"use client";

import { useRef, useEffect, useState, ChangeEvent, useActionState, useTransition } from "react";
import './newDropdown.css';
import '@/app/globals.css';
import { AddDocument } from "@/actions/documentActions";
import { Icon } from '@iconify/react';
import { DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { toast } from "sonner";
import { Roboto } from 'next/font/google'
import { FormResponse } from "@/types/return.type";
import { DocumentBase } from "@/types/document.type";

type UploadStatus =  "idle" | "uploading" | "succes" | "error";

const initialState: FormResponse<DocumentBase> = {
  success: false,
  message: "",
};

export default function NewButton() {

    const popupRef = useRef<HTMLDivElement | null>(null);       //Ref used to check if user clicks outside of popup
    const [status, setStatus] = useState<UploadStatus>("idle"); //upload status
    const [uploadPopup, setUploadPopup] = useState(false);      //bool which determines whether you can see the new popup
    const [newFile, setNewFile] = useState<File | null>(null);  //File for file upload
    const [docName, setDocName] = useState<string>("");         //File name
    const [docDescr, setDocDescr] = useState<string>("");       //File description
    const [state, action, isPending] = useActionState(AddDocument, initialState)
    const [isPending2, startTransition] = useTransition();


  //error messaging //

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
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
          console.log("New button clicked");
          setUploadPopup(true); //opens popup for file upload
  };
  
  const closeUploadPopup = () => { //When the popup closes values are reset
      setUploadPopup(false);
      setNewFile(null);
      setDocDescr("");
      setDocName("");
  } 


  useEffect(() => { //event listener on mouse used to close popup whenever a mouseclick occurs outside the popup
    if (uploadPopup) {
      document.addEventListener("mousedown", closePopup);
    }
    return () => {
      document.removeEventListener("mousedown", closePopup);
    };
  }, [uploadPopup]);


  //upload data updaters //
  function updateDescr(evt: any)
  {
    setDocDescr(evt.target.value);
  }

  function updateDocName(evt: any)
  {
    setDocName(evt.target.value)
  }


  //upload code //
  async function handleButton() { //is called when upload button is pressed
    if (!newFile) return;

    const b = docName == "";
    var dName: string;
    if (b) dName = newFile.name;
    else dName = docName;

    console.log(dName);
    console.log(docDescr);

    setStatus("uploading");
    const formData = new FormData();
    formData.append('file', newFile);
    formData.set("name", dName); 
    formData.set("description", docDescr) 

    try { 
      //Name, Description, IFormFile File, Overwrite(bool)
      startTransition(() => {
        AddDocument(initialState, formData);
      })
      setStatus("succes");
      setUploadPopup(false);
      console.log(status);
      }
    catch { setStatus("error"); }
  }

  
  function handleFileChange(e: ChangeEvent<HTMLInputElement>) { //is called when a file is selected from explorer
    if (e.target.files) {
      console.log(e.target.files[0])
      setNewFile(e.target.files[0]);
  
    }
  }


  return (
    <div>
      <DropdownMenu>
            {/* Purple New button */}
            <DropdownMenuTrigger className="font-face h-9 w-full bg-purple text-white text-md rounded-md text-left pl-3 
                           hover:bg-[#6f2aaf] active:bg-purple flex items-center gap-1 cursor-pointer">
                <Icon icon="mdi:plus-box" width="16" height="16"/>
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
        <div ref={popupRef} className="popup grid gridcols-2">

          {/* LEFT COLUMN */}
          <div className="col-[1] w-full">
            <div className="grid gridrows-4">

              <h2 className="row-[1] text-black pt-2.5 pl-5 font-bold text-2xl">Upload Document</h2>
              <div className="flex gap-3 row-[2] h-20">
                <div className="w-3/10 h-10 ml-5 mt-3 mb-2">

                  {/* Label is what you see however you click the input */}
                  <label htmlFor="file-Picker" className="labelCSS font-bold bg-[#E5E5E5] hover:bg-[#c9c2c2] rounded-xl flex items-center justify-center w-full h-full cursor-pointer" >Upload New File</label>
                  <input id="file-Picker" style={{visibility:"hidden", position:"absolute"}} 
                       type='file' onChange={handleFileChange} className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" />
                </div>
                <div className="mb-4 text-sm pl-5 pt-3">
                  {newFile && (
                  // information on uploaded file
                  <div>
                    <p>Type: {newFile.type}</p>
                    <p>Size: {(newFile.size / 1024).toFixed(2)} KB</p>
                  </div>
                  )}
                </div>
              </div>
              
              {/* Name and Author entries */}
              <div className="row-[3] text-xl font-semibold ml-5">
                <h2>File name: </h2>
                <input type="string" placeholder={newFile?.name} className="bg-slate-200 w-8/10 h-10 pl-2" onChange={evt => updateDocName(evt)}/>
                <h2 className="pt-2">Author name: </h2>
                <input type="string" className="bg-slate-200 w-8/10 h-10 pl-2"/>
              </div>
            
            </div>
          </div>


          {/* RIGHT COLUMN */}
          <div className="col-[2]">

            {/* Description entry */}
            <h2 className="pt-12 pl-3 text-xl font-semibold">Description: </h2>
            <textarea draggable='false' maxLength={512} placeholder="Description ..." className="bg-slate-200 w-9/10 m-3 h-5/10 pl-1 resize-none" onChange={evt => updateDescr(evt)}/>
            {/* Upload button */}
            <div className="pr-9">
              <button onClick={handleButton} className="float-right bg-[#E5E5E5] w-50 h-10 text-black text-lg font-bold rounded-xl hover:bg-[#c9c2c2] active:bg-[#E5E5E5]">
                  <div className='pb-0.5 cursor-pointer'>Upload</div>
              </button>
            </div>
          </div>


        </div>
      </div> 
      )}

    </div>
  );
};
