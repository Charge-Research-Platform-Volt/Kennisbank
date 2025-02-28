'use client';

import { Icon } from '@iconify/react';
import { useState } from "react";

export default function SideBar()
{
    const [nClicked, nSetClicked] = useState(false);

    const clickNew = () => {
        nSetClicked(!nClicked);
        console.log("New button clicked");
    };

    return (
    <div className="h-screen w-64 bg-slate-50">
        <button  onClick={clickNew}
                 className="w-full h-10 bg-purple-900 text-white text-lg font-semibold rounded-xl text-left pl-3 
                           hover:bg-purple-800 active:bg-purple-900 flex items-center gap-1">
            <Icon icon="mdi:plus-box" width="20" height="20"/>
            <div className='pb-0.5'>New</div>
        </button>
    </div>
    );
}




