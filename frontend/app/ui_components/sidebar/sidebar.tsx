'use client';

import { Icon } from '@iconify/react';
import { useState } from "react";
import UploadPopup from './upload';
import './sidebar.css';
import '../Colors.css';

export default function SideBar()
{
    const [uploadPopup, setUploadPopup] = useState(false);

    const clickNew = () => {
        console.log("New button clicked");
        setUploadPopup(true);
    };

    const closeUploadPopup = () => {
        setUploadPopup(false);
    }

    return (
    <div>
        <button  onClick={clickNew}
                 className="w-full h-10 bg-[#502379] text-white text-lg font-semibold rounded-xl text-left pl-3 
                           hover:bg-[#6f2aaf] active:bg-[#502379] flex items-center gap-1">
            <Icon icon="mdi:plus-box" width="20" height="20"/>
            <div className='pb-0.5'>New</div>
        </button>

        <UploadPopup isOpen={uploadPopup} onClose={closeUploadPopup} />
        
    </div>
    );
}




