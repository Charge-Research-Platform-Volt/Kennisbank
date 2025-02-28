import { useRef, useEffect } from "react";

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
    <div className="fixed inset-0 flex justify-center items-center bg-slate-300 bg-opacity-50">
      <div ref={popupRef} className="bg-white w-240 h-140 p-4 rounded-lg">
        <h2 className="text-black font-bold text-2xl">Upload Document</h2>
        <div className="border-4 border-dotted border-gray-600 w-180 h-90 flex justify-center items-center rounded-4xl">

        </div>
      </div>
    </div>
  );
};

export default Popup;
