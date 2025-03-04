import { useRef, useEffect, useState, ChangeEvent } from "react";
import './newDropdown.css';
import '@/app/globals.css';
import { Icon } from '@iconify/react';
import { DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger } from "@/components/ui/dropdown-menu";

type UploadStatus =  "idle" | "uploading" | "succes" | "error";

export default function NewButton() {
    
    const popupRef = useRef<HTMLDivElement | null>(null);
    const [status, setStatus] = useState<UploadStatus>("idle");
    const [uploadPopup, setUploadPopup] = useState(false);
    const [newFile, setFile] = useState<File | null>(null);
    //const [fileState, setFileState]

  //upload new file popup
  const closePopup = (e: MouseEvent) => {
    if (popupRef.current && !popupRef.current.contains(e.target as Node)) {
      closeUploadPopup();
    }
  };

  const clickNew = () => {
          console.log("New button clicked");
          setUploadPopup(true);
  };
  
  const closeUploadPopup = () => {
      setUploadPopup(false);
      setFile(null);
  } 


  useEffect(() => {
    if (uploadPopup) {
      document.addEventListener("mousedown", closePopup);
    }
    return () => {
      document.removeEventListener("mousedown", closePopup);
    };
  }, [uploadPopup]);


  //upload code

  async function handleButton() {
    if (!newFile) return;

    setStatus("uploading");
    const formData = new FormData();
    formData.append('file', newFile);

    try { 
      
      setStatus("succes"); }
    catch { setStatus("error"); }
  }

  
  function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    if (e.target.files) {
      console.log(e.target.files[0])
        setFile(e.target.files[0]);
  
    }
  }

  console.log(newFile?.name);


  return (
    <div>
      <DropdownMenu>
            <DropdownMenuTrigger className="newButton bg-purple text-white text-lg font-semibold rounded-xl text-left pl-3 
                           hover:bg-[#6f2aaf] active:bg-purple flex items-center gap-1">
                <Icon icon="mdi:plus-box" width="20" height="20"/>
                <div className='pb-0.5'>New</div>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
                <DropdownMenuItem className="w-auto" onClick={clickNew}>
                    <label htmlFor="filePicker" className="inline-block cursor-pointer">
                        Upload New Document
                    </label>
                <input id="filePicker" style={{visibility:"hidden", position:"absolute"}} type='file' onChange={handleFileChange} />
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                    Create New Project
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>

      {uploadPopup && (
      <div className="popupContainer">
        <div ref={popupRef} className="popup">
          <div className="flex gap-4">
            <h2 className="text-black pt-2.5 pl-5 font-bold text-2xl">Upload Document</h2>
          </div>
          {/* {true && (<div className="mb-4 text-sm pl-5">
                      <div className="flex gap-2 pt-1 justify-center items-center">
                        <p>Type: {newFile?.type}</p>
                        <p>Size: {(newFile?.size / 1024).toFixed(2)} KB</p>
                      </div>
                        <p>File name: {newFile.name}</p>
                  </div>)} */}
          <div className="pr-5">
            <button onClick={handleButton} className="float-right bg-[#E5E5E5] w-50 h-10 text-black text-lg font-bold rounded-xl hover:bg-[#c9c2c2] active:bg-[#E5E5E5]">
              <div className='pb-0.5'>Upload</div>
            </button>
          </div>
        </div>
      </div> 
      )}
    </div>
  );
};
