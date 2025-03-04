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
import { Separator } from "@radix-ui/react-dropdown-menu";

type UploadStatus =  "idle" | "uploading" | "succes" | "error";

export default function NewButton() {

    const popupRef = useRef<HTMLDivElement | null>(null);
    const [status, setStatus] = useState<UploadStatus>("idle");
    const [uploadPopup, setUploadPopup] = useState(false);
    const [newFile, setNewFile] = useState<File | null>(null);

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
      setNewFile(null);
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
      //Name, Description, IFormFile File, Overwrite(bool)
      //http put request await 
      setStatus("succes"); }
    catch { setStatus("error"); }
  }

  
  function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    if (e.target.files) {
      console.log(e.target.files[0])
      setNewFile(e.target.files[0]);
  
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
                    <label className="inline-block cursor-pointer">
                        Upload New Document
                    </label>
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                    Create New Project
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
                  <label htmlFor="file-Picker" className="labelCSS font-bold bg-[#E5E5E5] hover:bg-[#c9c2c2] rounded-xl flex items-center justify-center w-full h-full cursor-pointer" >Upload New File</label>
                  <input id="file-Picker" style={{visibility:"hidden", position:"absolute"}} 
                       type='file' onChange={handleFileChange} className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" />
                </div>
                <div className="mb-4 text-sm pl-5 pt-3">
                  {newFile && (
                  <div>
                    <p>Type: {newFile.type}</p>
                    <p>Size: {(newFile.size / 1024).toFixed(2)} KB</p>
                  </div>
                  )}
                </div>
              </div>

              <div className="row-[3] text-xl font-semibold ml-5">
                <h2>File name: </h2>
                <input type="string" placeholder={newFile?.name} className="bg-slate-200 w-8/10 h-10 pl-2"/>
                <h2 className="pt-2">Author name: </h2>
                <input type="string" className="bg-slate-200 w-8/10 h-10 pl-2"/>
              </div>
            
            </div>
          </div>


          {/* RIGHT COLUMN */}
          <div className="col-[2]">
            <h2 className="pt-12 pl-3 text-xl font-semibold">Description: </h2>
            <textarea draggable='false' maxLength={512} placeholder="Description ..." className="bg-slate-200 w-9/10 m-3 h-5/10 pl-1"/>
            <div className="pr-9">
              <button onClick={handleButton} className="float-right bg-[#E5E5E5] w-50 h-10 text-black text-lg font-bold rounded-xl hover:bg-[#c9c2c2] active:bg-[#E5E5E5]">
                  <div className='pb-0.5'>Upload</div>
              </button>
            </div>
          </div>


        </div>
      </div> 
      )}

    </div>
  );
};
