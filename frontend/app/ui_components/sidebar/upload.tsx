import { useRef, useEffect } from "react";
import './uploadPopup.css';

interface PopupProps {
  isOpen: boolean;
  onClose: () => void;
}

const Popup: React.FC<PopupProps> = ({ isOpen, onClose }) => {
  const popupRef = useRef<HTMLDivElement | null>(null);

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

  return (
    <div className="popupContainer">
    <div ref={popupRef} className="popup">
    <h2 className="text-black pt-1 pl-5 font-bold text-2xl">Upload Document</h2>
    <div className='flex justify-center pt-15 items-center'>
        <div className="border-4 border-dotted border-gray-600 w-180 h-90 rounded-4xl">

        </div>
    </div>

    </div>
    </div>

  );
};

export default Popup;
