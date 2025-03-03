import { useRef, useEffect, useState, ChangeEvent } from "react";
import './uploadPopup.css';
import '../Colors.css';

interface PopupProps {
  isOpen: boolean;
  onClose: () => void;
}

type UploadStatus = "idle" | "uploading" | "succes" | "error";

const Popup: React.FC<PopupProps> = ({ isOpen, onClose }) => {
    
    const popupRef = useRef<HTMLDivElement | null>(null);
    const [file, setFile] = useState<File | null>(null);
    const [status, setStatus] = useState<UploadStatus>("idle");

  const closePopup = (e: MouseEvent) => {
    if (popupRef.current && !popupRef.current.contains(e.target as Node)) {
      onClose();
    }
  };

  useEffect(() => {
    if (isOpen) {
      document.addEventListener("mousedown", closePopup);
    }
    return () => {
      document.removeEventListener("mousedown", closePopup);
    };
  }, [isOpen]);

  if (!isOpen) return null;


  //upload code
  
  function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    if (e.target.files) {
        setFile(e.target.files[0]);
    }
  } 

  async function handleButton() {
    if (!file) return;

    setStatus("uploading");

    const formData = new FormData();
    formData.append('file', file);

    try {
        //await fetchrequest from backend
        setStatus("succes");

    } catch {
        setStatus("error");
    };
  }


  return (
    <div className="popupContainer">
        <div ref={popupRef} className="popup">
            <h2 className="text-black pt-1 pl-5 font-bold text-2xl">Upload Document</h2>
            <div className="pt-2">
                <input type='file' onChange={handleFileChange} className="w-180 text-black border-gray-600 font-bold text-2xl border-4 border-dashed rounded-4xl pl-4 hover:bg-slate-200 text-center " />
                {file && (<div className="mb-4 text-sm pt-2">
                    <p>File name: {file.name}</p>
                    <p>Size: {(file.size / 1024).toFixed(2)} KB</p>
                    <p>Type: {file.type}</p>
                </div>)}
            </div>
            <div className="pr-5 pt-96">
                <button onClick={handleButton} className="float-right bg-[#E5E5E5] w-50 h-10 text-black text-lg font-bold rounded-xl hover:bg-[#c9c2c2] active:bg-[#E5E5E5]">
                    <div className='pb-0.5'>Upload</div>
                </button>
            </div>
        </div>
    </div>

  );
};

export default Popup;
